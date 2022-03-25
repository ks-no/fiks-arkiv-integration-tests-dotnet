using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.Journalpost;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel;

namespace KS.Fiks.Arkiv.Integration.Tests
{
    public class JournalpostHentTests : IntegrationTestsBase
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
        public void Verify_Arkivmelding_With_Journalpost_Then_Verify_JournalpostHent_By_SystemID()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som hører sammen
            var testSessionId = Guid.NewGuid().ToString();
            
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost();

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
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.ValidateArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var arkivmeldingKvittering = ArkiveringSerializeHelper.DeSerializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            var systemId = arkivmeldingKvittering.RegistreringKvittering[0].SystemID; // Bruk SystemID som man fikk i kvittering

            var journalpostHent = MeldingGenerator.CreateJournalpostHent(systemId);
            
            var journalpostHentAsString = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.ValidateJournalpostHent(nyJournalpostAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.JournalpostHent, journalpostHentAsString, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            var journalpostHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, ArkivintegrasjonMeldingTypeV1.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.ValidateJournalpostHentResultat(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.SystemID.Value, systemId.Value);
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering[0].Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
        
        [Test]
        public void Verify_Arkivmelding_With_Journalpost_Then_Verify_JournalpostHent_By_EksternNoekkel()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
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
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.ValidateArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkel);
            
            var journalpostHentAsString = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.ValidateJournalpostHent(journalpostHentAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, ArkivintegrasjonMeldingTypeV1.JournalpostHent, journalpostHentAsString, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            var journalpostHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, ArkivintegrasjonMeldingTypeV1.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.ValidateJournalpostHentResultat(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering[0].Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
    }
}