using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    // Payloads exchanged with on-prem cores. Field names and shapes are part of the contract with
    // existing installations; only additive, optional changes are allowed.

    public class RemoteConnectPortResponse
    {
        private int _port;

        public int Port
        {
            get => _port + 1023;
            set => _port = value;
        }
    }

    public class CreateRemoteConnectPortObject
    {
        public Guid DriverId { get; set; }
        public string ServiceName { get; set; }
        public string TunnelingProtocol { get; set; }
    }

    public class CreateRemoteConnectObject
    {
        public string TargetSubDomain { get; set; }
    }

    public class RemoteConnectObject
    {
        public string SubDomain { get; set; }
        public string TunnelUrl { get; set; }

        /// <summary>Additive: the frps auth token, for cores that support frps token auth.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FrpsToken { get; set; }
    }

    public class SayHelloData
    {
        public string Rid { get; set; }
        public string Version { get; set; }
        public Guid ServerGuid { get; set; }
    }

    public class EmailData
    {
        public IList<string> To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

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
        public TimeSpan AudioDuration { get; set; }
    }
}
