using System;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public class FiksIOConfig
    {
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