using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public class FiksRequestMessageService
    {
        private readonly Guid _senderId;
        private FiksIOClient _client;
        private const int TTLMinutes = 5; 

        public FiksRequestMessageService(IConfigurationRoot configuration)
        {
            var config = FiksIOConfigurationBuilder.CreateFiksIOConfiguration(configuration);

            _client = new FiksIOClient(config);

            _senderId = config.KontoConfiguration.KontoId;
        }

        public Guid Send(Guid mottakerKontoId, string meldingsType, string payloadContent, string payloadFilename, List<KeyValuePair<string, FileStream>> attachments, string testSessionId)
        {
            var headere = new Dictionary<string, string>() { { "testSessionId", testSessionId } };
            var ttl = new TimeSpan(0, TTLMinutes, 0);
            var messageRequest = new MeldingRequest(_senderId, mottakerKontoId, meldingsType, ttl, headere: headere);

            var payloads = new List<IPayload>();

            IPayload payload = new StringPayload(payloadContent, payloadFilename);
            payloads.Add(payload);

            if(attachments != null)
            {
                foreach (var (filename, fileStream) in attachments)
                {
                    payloads.Add(new StreamPayload(fileStream, filename));
                }    
            }

            var result = _client.Send(messageRequest, payloads).Result;

            return result.MeldingId;
        }
    }
}
