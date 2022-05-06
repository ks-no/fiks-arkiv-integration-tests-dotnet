using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.Protokoller.V1.Models.Feilmelding;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.InnsynTests
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en journalpost for så
     * hente journalposten igjen vha enten referanseEksternNoekkel eller systemID
     */
    public class HentJournalpostTests : IntegrationTestsBase
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
        public void Hent_Journalpost_Med_SystemID()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som hører sammen
            var testSessionId = Guid.NewGuid().ToString();
            
            // STEG 1: Opprett arkivmelding og send inn
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost();
            
            var nyJournalpostSerialized = ArkiveringSerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();

            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);
            
            File.WriteAllText("ArkivmeldingMedNyJournalpost.xml", nyJournalpostSerialized);

            // Send arkiver melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var arkivmeldingKvittering = ArkiveringSerializeHelper.DeSerializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            var systemId = arkivmeldingKvittering.RegistreringKvittering[0].SystemID; // Bruk SystemID som man fikk i kvittering

            // STEG 2: Henting av journalpost
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(systemId);
            
            var journalpostHentSerialized = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            File.WriteAllText("HentJournalpost.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.JournalpostHent, journalpostHentSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            var journalpostHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivV1Meldingtype.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.SystemID.Value, systemId.Value);
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering[0].Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }

        [Test] public void Hent_Journalpost_Med_EksternNoekkel()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest hent journalpost",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            // STEG 1: Opprett arkivmelding og send inn
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel);

            var nyJournalpostSerialized = ArkiveringSerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);
            
            File.WriteAllText("ArkivmeldingMedNyJournalpostEksternNoekkel.xml", nyJournalpostSerialized);

            // Send arkivering melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            // STEG 2: Henting av journalpost
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkel);
            
            var journalpostHentSerialized = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            File.WriteAllText("HentJournalpostEksternNoekkel.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.JournalpostHent, journalpostHentSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            var journalpostHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivV1Meldingtype.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            File.WriteAllText("HentJournalpostResultatEksternNoekkel.xml", journalpostHentSerialized);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering[0].Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
        
        [Test] public void Hent_Journalpost_Med_Ikke_Eksisterende_EksternNoekkel_Returnerer_Ikkefunnet()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest hent journalpost",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            // STEG 1: Opprett arkivmelding og send inn
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel);

            var nyJournalpostSerialized = ArkiveringSerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);
            
            // Send arkivering melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            var mottattMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivV1Meldingtype.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var referanseEksternNoekkelIkkeGyldig = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest hent journalpost men ikke gyldig",
                Noekkel = Guid.Empty.ToString()
            };
            
            // STEG 2: Henting av journalpost
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkelIkkeGyldig);
            
            var journalpostHentSerialized = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            File.WriteAllText("HentJournalpostEksternNoekkelIkkeGyldig.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivV1Meldingtype.JournalpostHent, journalpostHentSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får en Ikkefunnet feil-melding
            var ikkefunnetFeilmelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, FeilmeldingType.Ikkefunnet);

            Assert.IsNotNull(ikkefunnetFeilmelding);
            
            //TODO hent feilmelding og sjekk innhold?
        }
    }
}