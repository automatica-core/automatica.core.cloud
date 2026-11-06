using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud.WebApi.Services
{
    public interface IMailSender
    {
        Task SendAsync(string to, string subject, string plainText, string html, CancellationToken token = default);
    }

    public class AcsMailSender : IMailSender
    {
        private readonly IConfiguration _config;
        private readonly Lazy<EmailClient> _client;

        public AcsMailSender(IConfiguration config)
        {
            _config = config;
            _client = new Lazy<EmailClient>(CreateClient);
        }

        private EmailClient CreateClient()
        {
            var value = _config.GetConnectionString("AutomaticaAzureEmailCommunication");
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("ConnectionStrings:AutomaticaAzureEmailCommunication is not configured");
            }
            // "https://<acs>.communication.azure.com" = managed identity, otherwise a classic connection string
            if (Uri.TryCreate(value, UriKind.Absolute, out var endpoint) && endpoint.Scheme == Uri.UriSchemeHttps)
            {
                return new EmailClient(endpoint, new DefaultAzureCredential());
            }
            return new EmailClient(value);
        }

        public async Task SendAsync(string to, string subject, string plainText, string html, CancellationToken token = default)
        {
            var sender = _config["Mail:SenderAddress"];
            if (string.IsNullOrEmpty(sender))
            {
                sender = "cloud@automaticacore.com";
            }

            var message = new EmailMessage(sender, to, new EmailContent(subject) { PlainText = plainText, Html = html });
            await _client.Value.SendAsync(WaitUntil.Started, message, token);
        }
    }

    /// <summary>Turns whatever a core sends into plain text and wraps it in the one fixed template.</summary>
    public static partial class MailTemplates
    {
        public const int MaxBodyLength = 20_000;
        public const int MaxSubjectLength = 200;

        [GeneratedRegex("<(script|style)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ScriptOrStyle();

        [GeneratedRegex("<br\\s*/?>|</p>|</div>|</li>|</tr>|</h[1-6]>", RegexOptions.IgnoreCase)]
        private static partial Regex LineBreakTags();

        [GeneratedRegex("<[^>]*>")]
        private static partial Regex AnyTag();

        [GeneratedRegex("[\\r\\n\\t]+")]
        private static partial Regex ControlWhitespace();

        public static string ToPlainText(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return string.Empty;
            }
            var text = ScriptOrStyle().Replace(body, string.Empty);
            text = LineBreakTags().Replace(text, "\n");
            text = AnyTag().Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text).Trim();
            return text.Length > MaxBodyLength ? text[..MaxBodyLength] : text;
        }

        public static string SanitizeSubject(string subject)
        {
            var value = ControlWhitespace().Replace(subject ?? string.Empty, " ").Trim();
            if (value.Length == 0)
            {
                value = "Message from your Automatica.Core server";
            }
            return value.Length > MaxSubjectLength ? value[..MaxSubjectLength] : value;
        }

        public static string CoreMessageHtml(string serverName, string plainText)
        {
            var server = WebUtility.HtmlEncode(serverName ?? "Automatica.Core");
            var body = WebUtility.HtmlEncode(plainText);
            return "<!doctype html><html><body style=\"margin:0;padding:24px;background:#f3f5f5;font-family:Segoe UI,Arial,sans-serif;color:#172326\">" +
                   "<div style=\"max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #d3dcde;border-radius:10px;padding:24px\">" +
                   $"<p style=\"margin:0 0 16px;font-size:12px;color:#5b6b70\">Message from your Automatica.Core server <strong>{server}</strong></p>" +
                   $"<div style=\"white-space:pre-wrap;font-size:14px;line-height:1.5\">{body}</div>" +
                   "</div><p style=\"max-width:600px;margin:12px auto 0;font-size:11px;color:#5b6b70\">You get this mail because this address is registered for the server in Automatica Cloud.</p>" +
                   "</body></html>";
        }

        public static string VerificationHtml(string serverName, string link)
        {
            var server = WebUtility.HtmlEncode(serverName ?? "Automatica.Core");
            var href = WebUtility.HtmlEncode(link);
            return "<!doctype html><html><body style=\"margin:0;padding:24px;background:#f3f5f5;font-family:Segoe UI,Arial,sans-serif;color:#172326\">" +
                   "<div style=\"max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #d3dcde;border-radius:10px;padding:24px\">" +
                   $"<p style=\"font-size:14px\">Please confirm that the Automatica.Core server <strong>{server}</strong> may send notifications to this address.</p>" +
                   $"<p><a href=\"{href}\" style=\"display:inline-block;background:#2f6f7c;color:#ffffff;text-decoration:none;padding:10px 16px;border-radius:6px\">Confirm address</a></p>" +
                   "<p style=\"font-size:12px;color:#5b6b70\">If you did not expect this mail, ignore it. The link expires in 48 hours.</p>" +
                   "</div></body></html>";
        }
    }
}
