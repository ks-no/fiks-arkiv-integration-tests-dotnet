using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers
{
    public class MeldingHelper
    {
        public static async Task<PayloadFile> GetDecryptedMessagePayload(MottattMeldingArgs? mottattMeldingArgs)
        {
            return GetDecryptedPayloads(mottattMeldingArgs).Result[0];
        }

        private static async Task<List<PayloadFile>> GetDecryptedPayloads(MottattMeldingArgs? mottattMeldingArgs)
        {
            var payloadFiles = new List<PayloadFile>();

            Debug.Assert(mottattMeldingArgs != null, nameof(mottattMeldingArgs) + " != null");
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
                            payloadFiles.Add(new PayloadFile(asiceReadEntry.FileName, xml ));
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

    }
}