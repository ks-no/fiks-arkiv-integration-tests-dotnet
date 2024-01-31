using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public class FiksRequestMessageService
    {
        private readonly Guid _senderId;
        private FiksIOClient _client;
        private const int TTLMinutes = 5;
        private readonly FiksIOConfiguration _fiksIoConfiguration;

        public FiksRequestMessageService(IConfigurationRoot configuration)
        {
            _fiksIoConfiguration = FiksIOConfigurationBuilder.CreateFiksIOConfiguration(configuration);
            _senderId = _fiksIoConfiguration.KontoConfiguration.KontoId;
            Initialization = InitializeAsync();
        }
        
        private async Task InitializeAsync()
        {
            _client = await FiksIOClient.CreateAsync(_fiksIoConfiguration);
        }
        
        private Task Initialization { get; set; }

        public async Task<Guid> Send(Guid mottakerKontoId, string meldingsType, string payloadContent, List<KeyValuePair<string, FileStream>>? attachments, string testSessionId, string payloadFilename = null)
        {
            await Initialization;
            if (payloadFilename == null)
            {
                payloadFilename = FiksArkivPayloadHelper.GetPayloadFilnavn(meldingsType);
            }
            
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

            var result = await _client.Send(messageRequest, payloads);

            return result.MeldingId;
        }
    }
}
