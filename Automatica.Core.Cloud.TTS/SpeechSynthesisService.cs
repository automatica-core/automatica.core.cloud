using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Cloud.Storage;
using Azure.Storage.Blobs.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Cloud.TTS
{
    internal class SpeechSynthesisService : ITextToSpeechService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SpeechSynthesisService> _logger;

        public SpeechSynthesisService(IConfiguration config, ILogger<SpeechSynthesisService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private string Setting(string key, string legacyKey)
        {
            var value = _config[$"TextToSpeech:{key}"];
            if (string.IsNullOrEmpty(value))
            {
                value = _config[legacyKey];
            }
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"TextToSpeech:{key} is not configured");
            }
            return value;
        }

        private static string RequestHash(string text, string language, string voice)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{language}\n{voice}\n{text}")));
        }

        public async Task<SpeechSynthesizeResponse> TextToSpeech(Guid serverId, Guid id, string text, string language, string? voice, CancellationToken token = default)
        {
            var apiKey = Setting("ApiKey", "TextToSpeech_speechApiKey");
            var region = Setting("Region", "TextToSpeech_speechRegion");

            var speechConfig = SpeechConfig.FromSubscription(apiKey, region);
            if (string.IsNullOrEmpty(voice))
            {
                voice = "not set";
                speechConfig.SpeechSynthesisVoiceName = language;
            }
            else
            {
                speechConfig.SpeechSynthesisVoiceName = $"{language}-{voice}";
            }

            var container = BlobContainers.Create(_config, "AutomaticaTextToSpeechStorage", "tts");
            var blob = container.GetBlobClient($"{serverId}-{id}.wav");
            var requestHash = RequestHash(text, language, voice);

            if (await blob.ExistsAsync(token))
            {
                BlobProperties properties = await blob.GetPropertiesAsync(cancellationToken: token);
                if (properties.Metadata.TryGetValue("requestHash", out var storedHash) && storedHash == requestHash &&
                    properties.Metadata.TryGetValue("audioLength", out var audioLength) && TimeSpan.TryParse(audioLength, out var length))
                {
                    return new SpeechSynthesizeResponse { Uri = blob.Uri.ToString(), AudioDuration = length };
                }

                await blob.DeleteIfExistsAsync(cancellationToken: token);
            }

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await speechSynthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                using var stream = new MemoryStream(result.AudioData);
                await blob.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "audio/wav" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "requestHash", requestHash },
                        { "audioLength", result.AudioDuration.ToString() }
                    }
                }, token);

                return new SpeechSynthesizeResponse { Uri = blob.Uri.ToString(), AudioDuration = result.AudioDuration };
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Speech synthesis canceled: {Reason} {ErrorCode} {ErrorDetails}", cancellation.Reason, cancellation.ErrorCode, cancellation.ErrorDetails);
                throw new InvalidOperationException($"Speech synthesis failed: {cancellation.ErrorCode}");
            }

            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }
    }
}
