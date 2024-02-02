using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.InnsynTests
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en journalpost for så
     * hente journalposten igjen vha enten referanseEksternNoekkel eller systemID
     */
    public class HentJournalpostTests : IntegrationTestsBase
    {
        [SetUp]
        public async Task Setup()
        {
            await Init();
        }
        
        [Test]
        public async Task Hent_Journalpost_Med_SystemID()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers. 
            var testSessionId = Guid.NewGuid().ToString();
            
            // STEG 1: Opprett arkivmelding og send inn
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(FagsystemNavn);
            
            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();

            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNyJournalpost.xml", nyJournalpostSerialized);

            // Send arkiver melding
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized, null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var arkivmeldingKvittering = SerializeHelper.DeserializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            var systemId = arkivmeldingKvittering.RegistreringKvittering.SystemID; // Bruk SystemID som man fikk i kvittering

            // STEG 2: Henting av journalpost
            var journalpostHent = RegistreringHentBuilder.Init().WithSystemID(systemId).Build();
            
            var journalpostHentSerialized = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("HentJournalpost.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentSerialized, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent melding
            var journalpostHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.SystemID.Value, systemId.Value);
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering.Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }

        [Test] public async Task Hent_Journalpost_Med_EksternNoekkel()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers. 
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest hent journalpost",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            /*
             * STEG 1:
             * Opprett arkivmelding med en journalpost og send inn
             */
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel, FagsystemNavn);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNyJournalpostEksternNoekkel.xml", nyJournalpostSerialized);

            // Send arkivering melding
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized, null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            /*
             * STEG 2:
             * Hent journalposten som ble opprettet i steg 1
             */
            var journalpostHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkel).Build();
            
            var journalpostHentSerialized = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("HentJournalpostEksternNoekkel.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentSerialized, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent melding
            var journalpostHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(journalpostHentResultatPayload.PayloadAsString);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("HentJournalpostResultatEksternNoekkel.xml", journalpostHentSerialized);
            
            Assert.AreEqual(journalpostHentResultat.Journalpost.Tittel, arkivmelding.Registrering.Tittel);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
    }
}