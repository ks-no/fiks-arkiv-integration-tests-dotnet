using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using KS.Fiks.IO.Client.Configuration;
using Ks.Fiks.Maskinporten.Client;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public static class FiksIOConfigurationBuilder
    {
        public static FiksIOConfiguration CreateFiksIOConfiguration(IConfigurationRoot config)
        {
            var ignoreSSLError = true;

            var accountConfiguration = new KontoConfiguration(
                kontoId: Guid.Parse(config["FiksIOConfig:FiksIoAccountId"]),
                privatNokkel: File.ReadAllText(config["FiksIOConfig:FiksIoPrivateKey"])
            );

            // Id and password for integration associated to the Fiks IO account.
            var integrationConfiguration = new IntegrasjonConfiguration(
                integrasjonId: Guid.Parse(config["FiksIOConfig:FiksIoIntegrationId"]),
                integrasjonPassord: config["FiksIOConfig:FiksIoIntegrationPassword"],
                scope: config["FiksIOConfig:FiksIoIntegrationScope"]);

            // ID-porten machine to machine configuration
            var maskinportenClientConfiguration = new MaskinportenClientConfiguration(
                audience: config["FiksIOConfig:MaskinPortenAudienceUrl"],
                tokenEndpoint: config["FiksIOConfig:MaskinPortenTokenUrl"],
                issuer: config["FiksIOConfig:MaskinPortenIssuer"],
                numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
                certificate: GetCertificate(config["FiksIOConfig:MaskinPortenCompanyCertificateThumbprint"], config["FiksIOConfig:MaskinPortenCompanyCertificatePath"], config["FiksIOConfig:MaskinPortenCompanyCertificatePassword"]));

            // Optional: Use custom api host (i.e. for connecting to test api)
            var apiConfiguration = new ApiConfiguration(
                scheme: config["FiksIOConfig:ApiScheme"],
                host: config["FiksIOConfig:ApiHost"],
                port: int.Parse(config["FiksIOConfig:ApiPort"]));

            var sslOption1 = ignoreSSLError
                ? new SslOption()
                {
                    Enabled = true,
                    ServerName = config["FiksIOConfig:AmqpHost"],
                    CertificateValidationCallback =
                        (RemoteCertificateValidationCallback) ((sender, certificate, chain, errors) => true)
                }
                : null;
            
            // Optional: Use custom amqp host (i.e. for connection to test queue)
            var amqpConfiguration = new AmqpConfiguration(
                host: config["FiksIOConfig:AmqpHost"],
                port: int.Parse(config["FiksIOConfig:AmqpPort"]),
                sslOption1,
                "Fiks-Arkiv Integration-Tests");

            // Combine all configurations
            return new FiksIOConfiguration(
                kontoConfiguration: accountConfiguration,
                integrasjonConfiguration: integrationConfiguration,
                maskinportenConfiguration: maskinportenClientConfiguration,
                apiConfiguration: apiConfiguration,
                amqpConfiguration: amqpConfiguration);
        }
        
        private static X509Certificate2 GetCertificate(string thumbprint, string path, string password)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return new X509Certificate2(File.ReadAllBytes(path), password);
            }
           
            var store = new X509Store(StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

            store.Close();

            return certificates.Count > 0 ? certificates[0] : null;
        }
    }
}