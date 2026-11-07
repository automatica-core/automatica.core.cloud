using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.TTS;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [AllowAnonymous, Route("webapi/v{version:apiVersion}/tts"), CoreServerApiKey(requireServerGuid: true), ApiVersion("2.0")]
    [EnableRateLimiting(RateLimits.Tts)]
    public partial class TextToSpeechController : BaseController
    {
        private const int MaxTextLength = 2000;

        private readonly ITextToSpeechService _ttsService;
        private readonly CoreContext _db;

        public TextToSpeechController(ITextToSpeechService ttsService, CoreContext db)
        {
            _ttsService = ttsService;
            _db = db;
        }

        [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
        private static partial Regex VoiceToken();

        [HttpPost, Route("{apiKey}/{serverGuid}")]
        public async Task<TextToSpeechResponse> SynthesizeText([FromBody] TextToSpeechRequest request)
        {
            var server = HttpContext.GetCoreServer();
            var license = await _db.Licenses.Where(a => a.This2CoreServer == server.ObjId).OrderByDescending(a => a.ExpiresAt).FirstOrDefaultAsync();

            if (license is not { AllowTextToSpeech: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > MaxTextLength ||
                string.IsNullOrEmpty(request.Language) || !VoiceToken().IsMatch(request.Language) ||
                (!string.IsNullOrEmpty(request.Voice) && !VoiceToken().IsMatch(request.Voice)))
            {
                throw new ArgumentException("Invalid text to speech request");
            }

            var retUrl = await _ttsService.TextToSpeech(server.ServerGuid, request.Id, request.Text, request.Language, request.Voice, HttpContext.RequestAborted);

            return new TextToSpeechResponse
            {
                Url = retUrl.Uri,
                AudioDuration = retUrl.AudioDuration
            };
        }
    }
}
