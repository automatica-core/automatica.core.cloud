using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.TTS;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class TextToSpeechRequest
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public string Language { get; set; }
        public string Voice { get; set; }
    }

    public class TextToSpeechResponse
    {
        public string Url { get; set; }
    }

    [AllowAnonymous, Route("webapi/v{version:apiVersion}/tts"), ServerApiKeyAuthorizationV2, ApiVersion("2.0")]
    public class TextToSpeechService : BaseController
    {
        private readonly ITextToSpeechService _ttsService;

        public TextToSpeechService(ITextToSpeechService ttsService, IConfiguration config) : base(config)
        {
            _ttsService = ttsService;
        }

        [HttpPost, Route("{apiKey}/{serverGuid}")]
        public async Task<TextToSpeechResponse> SynthesizeText(
            [FromBody] TextToSpeechRequest request, Guid apiKey)
        {
            await using var dbContext = new CoreContext(Config);
            var server = await CheckIfServerExistsAndIsValid(dbContext, apiKey, null);

            var license = dbContext.Licenses.SingleOrDefault(a => a.This2CoreServer == server.ObjId);

            if (license is not { AllowTextToSpeech: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var retUrl = await _ttsService.TextToSpeech(server.ServerGuid, request.Id, request.Text, request.Language, request.Voice);
           
            return new TextToSpeechResponse
            {
                Url = retUrl
            };
        }
    }
}
