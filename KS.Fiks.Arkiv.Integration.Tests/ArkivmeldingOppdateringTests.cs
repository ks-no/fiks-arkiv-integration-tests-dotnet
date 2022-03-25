using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.Journalpost;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests
{
    public class ArkivmeldingOppdateringTests : IntegrationTestsBase
    {
        //private static List<MottattMeldingArgs> _mottatMeldingArgsList;
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
        public void Verify_Arkivmelding_With_Journalpost_Then_Verify_ArkivmeldingOppdatering_By_EksternNoekkel()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen 
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel()
            {
                Fagsystem = "Integrasjonstest med eksternnoekkel",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel);

            var nyJournalpostAsString = ArkiveringSerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator(Directory.GetCurrentDirectory());
            
            // Valider arkivmelding
            validator.ValidateArkivmelding(nyJournalpostAsString);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.Arkivmelding, nyJournalpostAsString, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.ValidateArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);

            string nyTittel = "En helt ny tittel";
            var arkivmeldingOppdatering = MeldingGenerator.CreateArkivmeldingOppdatering(referanseEksternNoekkel, nyTittel);
            
            var arkivmeldingOppdateringAsString = ArkiveringSerializeHelper.Serialize(arkivmeldingOppdatering);
            
            // Valider innhold (xml)
            validator.ValidateArkivmeldingOppdatering(arkivmeldingOppdateringAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send melding
            var arkivmeldingOppdaterMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingOppdater, arkivmeldingOppdateringAsString, "arkivmelding.xml", null, testSessionId);

            // Vent på 2 respons meldinger. Mottat og kvittering 
            VentPaSvar(2, 10); 
            
            // Verifiser at man får mottatt melding
            GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får arkivmeldingOppdaterKvittering melding
            var arkivmeldingOppdaterKvittering = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, arkivmeldingOppdaterMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingOppdaterKvittering);

            Assert.IsNotNull(arkivmeldingOppdaterKvittering);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingOppdaterKvittering).Result;
            
            // Valider innhold (xml)
            validator.ValidateJournalpostHentResultat(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering[0].Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
    }
}