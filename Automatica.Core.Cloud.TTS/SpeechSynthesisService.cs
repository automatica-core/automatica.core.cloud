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

        public async Task<string> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice)
        {
            var speechConfig = SpeechConfig.FromEndpoint(new Uri("https://germanywestcentral.api.cognitive.microsoft.com/sts/v1.0/issuetoken"), _config["TextToSpeech:speechApiKey"]);
            speechConfig.SpeechSynthesisVoiceName = $"{language}-{voice}";

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            var result = await speechSynthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var container = GetCloudBlobContainer();

                var blob = container.GetBlockBlobReference($"{serverId}-{id}.wav"); 
                await blob.DeleteIfExistsAsync();

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