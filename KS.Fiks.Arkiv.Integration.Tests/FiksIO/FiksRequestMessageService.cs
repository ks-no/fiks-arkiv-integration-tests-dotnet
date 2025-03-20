using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using Microsoft.Extensions.Configuration;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
using OnSendCallback = Action<Guid, Guid,  string>;
    public class FiksRequestMessageService
    {
        private readonly Guid _senderId;
        private FiksIOClient _client;
        private const int TTLMinutes = 5;
        private readonly FiksIOConfiguration _fiksIoConfiguration;
        private IEnumerable<OnSendCallback> onSendCallbacks = new List<OnSendCallback>();

        public FiksRequestMessageService(IConfigurationRoot configuration, OnSendCallback? cb = null)
        {
            _fiksIoConfiguration = FiksIOConfigurationBuilder.CreateFiksIOConfiguration(configuration);
            _senderId = _fiksIoConfiguration.KontoConfiguration.KontoId;
            Initialization = InitializeAsync();
            if (cb != null)
            {
                AddOnSendCallback(cb);
            } else
            {
                AddOnSendCallback((s, m, t) =>
                {
                    Console.Out.WriteLine($"{Environment.NewLine}📡 Sendte {MeldingHelper.ShortenMessageType(t)}-melding med id {m} til {s} ");
                });
                
            }
        }

        public void AddOnSendCallback(OnSendCallback cb)
        {
            onSendCallbacks= onSendCallbacks.Append(cb);
        }
        
        private async Task InitializeAsync()
        {
            _client = await FiksIOClient.CreateAsync(_fiksIoConfiguration);
        }
        
        private Task Initialization { get; set; }

        public async Task<Guid> SendAsync(Guid mottakerKontoId, string meldingsType, string payloadContent, List<MeldingAttachment>? attachments, string testSessionId, string payloadFilename = null)
        {
            var stackTrace = new StackTrace();
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
                payloads.AddRange(attachments.Select(attachment => new StreamPayload(attachment.Filestream, attachment.Filename)).Cast<IPayload>());
            }

            var result = await _client.Send(messageRequest, payloads);

            if (onSendCallbacks.Any())
            {
                onSend(mottakerKontoId, result.MeldingId,  meldingsType, stackTrace);
            }
            return result.MeldingId;
        }

        private void onSend(Guid sentId, Guid mottakerId, string meldingstype, StackTrace stackTrace)
        {
            foreach (var cb in onSendCallbacks)
            {
                cb(sentId, mottakerId, meldingstype);
            }
        }
    }
}
