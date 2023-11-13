

using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Cloud.TTS
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddTextToSpeechProvider(this IServiceCollection serviceProvider)
        {

            serviceProvider.AddScoped<ITextToSpeechService, SpeechSynthesisService>();
            return serviceProvider;
        }
     
    }
}
