using System;
using System.Collections.Generic;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.ArkivmeldingOppdateringTests
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en journalpost for så
     * oppdatere journalposten ved oppdater-meldinger, og til slutt hente journalposten igjen vha enten referanseEksternNoekkel eller systemID
     * for å bekrefte at journalpost er endret som ønsket
     */
    public class OppdaterOgHentJournalpostTests : IntegrationTestsBase
    {
        [SetUp]
        public void Setup()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste. 
            _mottatMeldingArgsList = new List<MottattMeldingArgs>();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Local.json")
                .Build();
            _client = new FiksIOClient(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            _client.NewSubscription(OnMottattMelding);
            _fiksRequestService = new FiksRequestMessageService(config);
            _mottakerKontoId = Guid.Parse(config["TestConfig:ArkivAccountId"]);
        }
        
        [Test]
        public void Oppdater_Tittel_Journalpost()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = "Integrasjonstest for arkivmeldingoppdatering med eksternnoekkel",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            /*
             * STEG 1:
             * Opprett arkivmelding og send inn
             */
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Send oppdatering av journalposten som ble opprettet i steg 1
             */
            
            var nyTittel = "En helt ny tittel";
            var arkivmeldingOppdatering = MeldingGenerator.CreateArkivmeldingOppdateringRegistreringOppdateringNyTittel(referanseEksternNoekkel, nyTittel);
            
            var arkivmeldingOppdateringSerialized = SerializeHelper.Serialize(arkivmeldingOppdatering);
            
            // Valider innhold (xml)
            validator.Validate(arkivmeldingOppdateringSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send oppdater melding
            var arkivmeldingOppdaterMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOppdater, arkivmeldingOppdateringSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 2 respons meldinger. Mottat og kvittering 
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding på oppdatering
            SjekkForventetMelding(_mottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterMottatt);

            // Verifiser at man får arkivmeldingOppdaterKvittering melding
            SjekkForventetMelding(_mottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterKvittering);
            
            //Hent melding
            var arkivmeldingOppdaterKvittering = GetMottattMelding(_mottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterKvittering);

            Assert.IsNotNull(arkivmeldingOppdaterKvittering);
            
            /*
             * STEG 3:
             * Hent journalpost som har blitt oppdatert og bekreft at den er oppdatert
             */
            
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkel);
            
            var journalpostHentAsString = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentAsString, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            SjekkForventetMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent melding
            var journalpostHentResultatMelding = GetMottattMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(journalpostHentResultatPayload.PayloadAsString);

            Assert.AreEqual(nyTittel, journalpostHentResultat.Journalpost.Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
    }
}