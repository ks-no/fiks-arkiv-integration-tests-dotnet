using System;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public class FiksIOConfig
    {
        public FiksIOConfig(string apiHost, int apiPort, string apiScheme, string amqpHost, int amqpPort, Guid fiksIoAccountId, Guid fiksIoIntegrationId, string fiksIoIntegrationPassword, string fiksIoIntegrationScope, string fiksIoPrivateKey, string maskinPortenAudienceUrl, string maskinPortenCompanyCertificateThumbprint, string maskinPortenCompanyCertificatePath, string maskinPortenCompanyCertificatePassword, string maskinPortenIssuer, string maskinPortenTokenUrl)
        {
            ApiHost = apiHost;
            ApiPort = apiPort;
            ApiScheme = apiScheme;
            AmqpHost = amqpHost;
            AmqpPort = amqpPort;
            FiksIoAccountId = fiksIoAccountId;
            FiksIoIntegrationId = fiksIoIntegrationId;
            FiksIoIntegrationPassword = fiksIoIntegrationPassword;
            FiksIoIntegrationScope = fiksIoIntegrationScope;
            FiksIoPrivateKey = fiksIoPrivateKey;
            MaskinPortenAudienceUrl = maskinPortenAudienceUrl;
            MaskinPortenCompanyCertificateThumbprint = maskinPortenCompanyCertificateThumbprint;
            MaskinPortenCompanyCertificatePath = maskinPortenCompanyCertificatePath;
            MaskinPortenCompanyCertificatePassword = maskinPortenCompanyCertificatePassword;
            MaskinPortenIssuer = maskinPortenIssuer;
            MaskinPortenTokenUrl = maskinPortenTokenUrl;
        }

        public string ApiHost { get; set; } 
        public int ApiPort { get; set; }
        public string ApiScheme { get; set; }
        public string AmqpHost { get; set; }
        public int AmqpPort { get; set; }
        public Guid FiksIoAccountId { get; set; }
        public Guid FiksIoIntegrationId { get; set; }
        public string FiksIoIntegrationPassword { get; set; }
        public string FiksIoIntegrationScope { get; set; }
        public string FiksIoPrivateKey { get; set; }
        public string MaskinPortenAudienceUrl { get; set; }
        public string MaskinPortenCompanyCertificateThumbprint { get; set; }
        public string MaskinPortenCompanyCertificatePath { get; set; }
        public string MaskinPortenCompanyCertificatePassword { get; set; }
        public string MaskinPortenIssuer { get; set; }
        public string MaskinPortenTokenUrl { get; set; }
    }
}