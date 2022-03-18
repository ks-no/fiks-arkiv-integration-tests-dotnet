using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests
{
    public class ArkiveringTests
    {
        private static List<MottattMeldingArgs> _mottatMeldingArgsList;
        private IFiksIOClient _client;
        private FiksRequestMessageService _fiksRequestService;
        private Guid _mottakerKontoId;

        [SetUp]
        public void Setup()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste. 
            _mottatMeldingArgsList = new List<MottattMeldingArgs>();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();
            _client = new FiksIOClient(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            _client.NewSubscription(OnMottattMelding);
            _fiksRequestService = new FiksRequestMessageService(config);
            _mottakerKontoId = Guid.Parse(config["TestConfig:ArkivAccountId"]);
        }
        
        [Test]
        public void Verify_NyJournalpost_Then_Verify_JournalpostHent_By_SystemID()
        {
            var nyJournalpost = MeldingFactory.CreateNyJournalpostMelding();

            var nyJournalpostAsString = ArkivmeldingSerializeHelper.Serialize(nyJournalpost);
            var validator = new SimpleXsdValidator(Directory.GetCurrentDirectory());
            
            // Valider arkivmelding
            validator.ValidateArkivmelding(nyJournalpostAsString);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.Arkivmelding, nyJournalpostAsString, "arkivmelding.xml", null);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            var counter = 0;
            while(_mottatMeldingArgsList.Count <= 2 && counter < 10)
            {
                Thread.Sleep(400);
                counter++;
            }

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering);
            
            var payloads = MeldingHelper.GetPayloads(arkivmeldingKvitteringMelding).Result;

            var arkivmeldingKvitteringPayload = payloads[0];
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider arkivmeldingKvittering innhold (xml)
            validator.ValidateArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var arkivmeldingKvittering = ArkivmeldingSerializeHelper.DeSerializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            var systemId = arkivmeldingKvittering.RegistreringKvittering[0].SystemID;

            var journalpostHent = MeldingFactory.CreateJournalpostHent(systemId);
            
            var journalpostHentAsString = ArkivmeldingSerializeHelper.Serialize(journalpostHent);
            
            // Valider journalpostHent innhold (xml)
            validator.ValidateJournalpostHent(nyJournalpostAsString);
            
            // Send melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.JournalpostHent, journalpostHentAsString, "arkivmelding.xml", null);

            // Vent på 2 første response meldinger (mottatt og resultat)
            counter = 0;
            while(_mottatMeldingArgsList.Count <= 2 && counter < 10)
            {
                Thread.Sleep(400);
                counter++;
            }

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får arkivmeldingKvittering melding
            var journalpostHentResultat = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, ArkivintegrasjonMeldingTypeV1.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultat);
        }

        private static async void OnMottattMelding(object sender, MottattMeldingArgs mottattMeldingArgs)
        {
            await Console.Out.WriteLineAsync($"Mottatt melding med MeldingId: {mottattMeldingArgs.Melding.MeldingId}, SvarPaMeldingId: {mottattMeldingArgs.Melding.SvarPaMelding}, MeldingType: {mottattMeldingArgs.Melding.MeldingType} og lagrer i listen");
            _mottatMeldingArgsList.Add(mottattMeldingArgs);
            mottattMeldingArgs.SvarSender?.Ack();
        }

        private static MottattMeldingArgs GetAndVerifyByMeldingstype(List<MottattMeldingArgs> mottattMeldingArgsList, Guid sendtMeldingid, string forventetMeldingstype)
        {
            foreach (var mottatMeldingArgs in mottattMeldingArgsList)
            {
                if (mottatMeldingArgs.Melding.SvarPaMelding == sendtMeldingid)
                {
                    Console.Out.WriteLineAsync($"Svar på vår melding med meldingId {sendtMeldingid} funnet. Melding er av typen: {mottatMeldingArgs.Melding.MeldingType}");
                    Assert.False(mottatMeldingArgs.Melding.MeldingType is FeilmeldingMeldingTypeV1.Ugyldigforespørsel or FeilmeldingMeldingTypeV1.Serverfeil, $"Uforventet svar av typen {mottatMeldingArgs.Melding.MeldingType}");
                    
                    if (mottatMeldingArgs.Melding.MeldingType == forventetMeldingstype)
                    {
                        Console.Out.WriteLineAsync($"Forventet meldingstype {forventetMeldingstype} funnet!");
                        mottattMeldingArgsList.Remove(mottatMeldingArgs);
                        return mottatMeldingArgs;
                    }
                }        
            }

            return null;
        }
    }
}