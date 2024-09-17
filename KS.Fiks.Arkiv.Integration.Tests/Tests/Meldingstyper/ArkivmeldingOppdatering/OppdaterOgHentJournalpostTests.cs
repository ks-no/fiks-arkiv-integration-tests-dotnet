using System;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Meldingstyper.ArkivmeldingOppdatering
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en journalpost for så
     * oppdatere journalposten ved oppdater-meldinger, og til slutt hente journalposten igjen vha enten referanseEksternNoekkel eller systemID
     * for å bekrefte at journalpost er endret som ønsket
     */
    public class OppdaterOgHentJournalpostTests : IntegrationTestsBase
    {
        [SetUp]
        public async Task Setup()
        {
            await Init();
        }
        
        [Test]
        public async Task Oppdater_Tittel_Journalpost()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = FagsystemNavn,
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            /*
             * STEG 1:
             * Opprett arkivmelding og send inn
             */
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(referanseEksternNoekkel, FagsystemNavn);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized,  null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Send oppdatering av journalposten som ble opprettet i steg 1
             */
            
            var nyTittel = "En helt ny tittel";
            var referanseTilRegistrering = new ReferanseTilRegistrering()
            {
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = referanseEksternNoekkel.Fagsystem,
                    Noekkel = referanseEksternNoekkel.Noekkel
                }
            };
            var arkivmeldingOppdatering = MeldingGenerator.CreateArkivmeldingOppdateringRegistreringOppdateringNyTittel(referanseTilRegistrering, nyTittel);
            
            var arkivmeldingOppdateringSerialized = SerializeHelper.Serialize(arkivmeldingOppdatering);
            
            // Valider innhold (xml)
            validator.Validate(arkivmeldingOppdateringSerialized);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send oppdater melding
            var arkivmeldingOppdaterMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOppdater, arkivmeldingOppdateringSerialized, null, testSessionId);

            // Vent på 2 respons meldinger. Mottat og kvittering 
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding på oppdatering
            SjekkForventetMelding(MottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterMottatt);

            // Verifiser at man får arkivmeldingOppdaterKvittering melding
            SjekkForventetMelding(MottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterKvittering);
            
            //Hent melding
            var arkivmeldingOppdaterKvittering = GetMottattMelding(MottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterKvittering);

            Assert.That(arkivmeldingOppdaterKvittering != null);
            
            /*
             * STEG 3:
             * Hent journalpost som har blitt oppdatert og bekreft at den er oppdatert
             */
            
            var journalpostHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkel).Build();
            
            var journalpostHentAsString = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent melding
            var journalpostHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.That(journalpostHentResultatMelding != null);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(journalpostHentResultatPayload.PayloadAsString);

            Assert.That(nyTittel == journalpostHentResultat.Journalpost.Tittel);
            Assert.That(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel == arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
    }
}