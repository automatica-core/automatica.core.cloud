namespace Automatica.Core.Cloud.TTS;

public class SpeechSynthesizeResponse
{
    public string Uri { get; set; } = string.Empty;
    public TimeSpan AudioDuration { get; set; }
}