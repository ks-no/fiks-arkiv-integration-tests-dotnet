using System;
using System.Collections.Generic;
using System.Threading;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests
{
    public class IntegrationTestsBase
    {
        protected static List<MottattMeldingArgs> _mottatMeldingArgsList;
        protected IFiksIOClient? _client;
        protected FiksRequestMessageService? _fiksRequestService;
        protected Guid _mottakerKontoId;
        
        protected static void VentPaSvar(int antallForventet, int antallVenter)
        {
            var counter = 0;
            while (_mottatMeldingArgsList.Count <= antallForventet && counter < antallVenter)
            {
                Thread.Sleep(500);
                counter++;
            }
        }

        protected static async void OnMottattMelding(object sender, MottattMeldingArgs mottattMeldingArgs)
        {
            await Console.Out.WriteLineAsync($"Mottatt melding med MeldingId: {mottattMeldingArgs.Melding.MeldingId}, SvarPaMeldingId: {mottattMeldingArgs.Melding.SvarPaMelding}, MeldingType: {mottattMeldingArgs.Melding.MeldingType} og lagrer i listen");
            _mottatMeldingArgsList.Add(mottattMeldingArgs);
            mottattMeldingArgs.SvarSender?.Ack();
        }

        protected static MottattMeldingArgs? GetAndVerifyByMeldingstype(List<MottattMeldingArgs> mottattMeldingArgsList, Guid sendtMeldingid, string forventetMeldingstype)
        {
            foreach (var mottatMeldingArgs in mottattMeldingArgsList)
            {
                if (mottatMeldingArgs.Melding.SvarPaMelding == sendtMeldingid)
                {
                    Console.Out.WriteLineAsync($"Svar på vår melding med meldingId {sendtMeldingid} mottatt. Melding er av typen: {mottatMeldingArgs.Melding.MeldingType}");
                    Assert.False(mottatMeldingArgs.Melding.MeldingType is FeilmeldingMeldingTypeV1.Ugyldigforespørsel or FeilmeldingMeldingTypeV1.Serverfeil, $"Uforventet svar av typen {mottatMeldingArgs.Melding.MeldingType}");
                    
                    if (mottatMeldingArgs.Melding.MeldingType == forventetMeldingstype)
                    {
                        Console.Out.WriteLineAsync($"Forventet meldingstype {forventetMeldingstype} mottatt!");
                        mottattMeldingArgsList.Remove(mottatMeldingArgs);
                        return mottatMeldingArgs;
                    }
                }        
            }

            return null;
        }
    }
}