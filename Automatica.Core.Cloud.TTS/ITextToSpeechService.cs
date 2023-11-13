namespace Automatica.Core.Cloud.TTS
{
    public interface ITextToSpeechService
    {
        Task<string> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice);
    }
}
