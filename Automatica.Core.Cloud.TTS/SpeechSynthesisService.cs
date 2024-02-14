using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Web;

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

        public async Task<SpeechSynthesizeResponse> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice, CancellationToken token = default)
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
                var blobText = HttpUtility.HtmlEncode(blob.Metadata["text"]);
                var blobLanguage = HttpUtility.HtmlEncode(blob.Metadata["language"]);
                var blobVoice = HttpUtility.HtmlEncode(blob.Metadata["voice"]);

                if (blob.Metadata.TryGetValue("audioLength", out var audioLength))
                {
                    var blobLength = TimeSpan.Parse(HttpUtility.HtmlEncode(audioLength));
                    if (blobText == text && blobLanguage == language && blobVoice == voice)
                    {
                        return new SpeechSynthesizeResponse
                        {
                            Uri = blob.Uri.ToString(),
                            AudioDuration = blobLength
                        };
                    }
                }

                await blob.DeleteIfExistsAsync();
            }
            blob.Metadata["text"] = HttpUtility.HtmlEncode(text);
            blob.Metadata["language"] = HttpUtility.HtmlEncode(language);
            blob.Metadata["voice"] = HttpUtility.HtmlEncode(voice);

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            var result = await speechSynthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                blob.Metadata["audioLength"] = result.AudioDuration.ToString(); 
                await blob.UploadFromByteArrayAsync(result.AudioData, 0, result.AudioData.Length);
                return new SpeechSynthesizeResponse
                {
                    Uri = blob.Uri.ToString(),
                    AudioDuration = result.AudioDuration
                };

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