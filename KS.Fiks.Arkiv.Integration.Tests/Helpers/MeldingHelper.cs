using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;

namespace KS.Fiks.Arkiv.Integration.Tests.Helpers
{
    public class MeldingHelper
    {
        public static async Task<PayloadFile> GetDecryptedMessagePayload(MottattMeldingArgs? mottattMeldingArgs)
        {
            var result = await GetDecryptedPayloads(mottattMeldingArgs);
            var firstPayloadFile = result[0];
            return firstPayloadFile;
        }

        private static async Task<List<PayloadFile>> GetDecryptedPayloads(MottattMeldingArgs? mottattMeldingArgs)
        {
            var payloadFiles = new List<PayloadFile>();

            if (mottattMeldingArgs.Melding.HasPayload)
            {
                try
                {
                    IAsicReader reader = new AsiceReader();
                    await using var inputStream = mottattMeldingArgs.Melding.DecryptedStream.Result;
                    using var asice = reader.Read(inputStream);
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        await using var entryStream = asiceReadEntry.OpenStream();
                        using (var ms = new MemoryStream())
                        {
                            var streamReader = new StreamReader(entryStream);
                            var xml = streamReader.ReadToEndAsync().Result;
                            payloadFiles.Add(new PayloadFile(asiceReadEntry.FileName, xml));
                        }
                    }
                }
                catch (Exception e)
                {
                    await Console.Out.WriteLineAsync($"Klarte ikke hente payload og melding blir dermed ikke parset. MeldingId: {mottattMeldingArgs.Melding?.MeldingId}, Error: {e.Message}");
                    mottattMeldingArgs.SvarSender?.Ack();
                }
            }

            return payloadFiles;
        }

        public static string ShortenMessageType(MottattMeldingArgs mottattMeldingArgs) => 
            ShortenMessageType(mottattMeldingArgs.Melding.MeldingType);
        public static string ShortenMessageType(string MeldingType)
        {

            var last = MeldingType.LastIndexOf('.');
            return last >= 0
                ? MeldingType.Substring(last + 1)
                : MeldingType;
        }

    }
}
