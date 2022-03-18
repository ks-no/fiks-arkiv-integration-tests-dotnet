using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers
{
    public class MeldingHelper
    {
        public static async Task<List<PayloadFile>> GetPayloads(MottattMeldingArgs mottattMeldingArgs)
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
                            payloadFiles.Add(new PayloadFile() { Filename = asiceReadEntry.FileName, PayloadAsString = xml });
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