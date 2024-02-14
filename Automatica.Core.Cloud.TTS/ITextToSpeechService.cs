namespace Automatica.Core.Cloud.TTS
{
    public interface ITextToSpeechService
    {
        Task<SpeechSynthesizeResponse> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice, CancellationToken token = default);
    }
}
