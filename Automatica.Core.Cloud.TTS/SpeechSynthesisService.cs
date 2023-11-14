using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

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

        public async Task<string> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice, CancellationToken token = default)
        {
            var client = new HttpClient();
            var apiKey = _config["TextToSpeech_speechApiKey"];
            var url = _config["TextToSpeech_speechUri"]!;
            var region = _config["TextToSpeech_speechRegion"]!;


            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var response = await client.PostAsync(url, null, token);
            var authToken = await response.Content.ReadAsStringAsync(token);

            var speechConfig = SpeechConfig.FromAuthorizationToken(authToken, region);
            speechConfig.SetProperty(PropertyId.Speech_LogFilename, "speech_log.txt");

            if (String.IsNullOrEmpty(voice))
            {
                voice = "not set";
                speechConfig.SpeechSynthesisVoiceName = $"{language}";
            }
            else
            {
                speechConfig.SpeechSynthesisVoiceName = $"{language}-{voice}";
            }

            var container = GetCloudBlobContainer();

            var blob = container.GetBlockBlobReference($"{serverId}-{id}.wav");
            var exists = await blob.ExistsAsync();
            if (exists)
            {
                var blobText = blob.Metadata["text"];
                var blobLanguage = blob.Metadata["language"];
                var blobVoice = blob.Metadata["voice"];

                if (blobText == text && blobLanguage == language && blobVoice == voice)
                {
                    return blob.Uri.ToString();
                }
                await blob.DeleteIfExistsAsync();
            }
            blob.Metadata["text"] = text;
            blob.Metadata["language"] = language;
            blob.Metadata["voice"] = voice;

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            var result = await speechSynthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {

                await blob.UploadFromByteArrayAsync(result.AudioData, 0, result.AudioData.Length);
                return blob.Uri.ToString();

            }
            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    _logger.LogError($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    _logger.LogError($"CANCELED: Did you update the subscription info?");

                    throw new ArgumentException($"{cancellation.ErrorCode} {cancellation.ErrorDetails}");
                }
            }

            throw new ArgumentException($"{result.Reason}");
        }

        protected CloudBlobContainer GetCloudBlobContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(_config.GetConnectionString("AutomaticaTextToSpeechStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("tts");
            return container;
        }
    }
}