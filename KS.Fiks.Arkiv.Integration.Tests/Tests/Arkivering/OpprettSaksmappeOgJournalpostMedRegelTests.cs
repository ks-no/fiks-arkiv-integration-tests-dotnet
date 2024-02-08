using System;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Arkivering
{
    /**
     * Disse testene er for å opprette saksmappe og journalposter.
     */
    public class OpprettSaksmappeOgJournalpostMedRegelTests : IntegrationTestsBase
    {
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
        private const string ArkivmeldingRegel = "FiksArkiv Integrasjonstest regel";
        private EksternNoekkel _saksmappeEksternNoekkel;

        [SetUp]
        public async Task Setup()
        {
            await Init();
            
            validator = new SimpleXsdValidator();

            _saksmappeEksternNoekkel = GenererEksternNoekkel(SaksmappeEksternNoekkelNoekkel);
        }
        
        /*
         * Denne testen oppretter saksmappe hvis den ikke finnes og ny journalpost med et hoveddokument på saksmappen.
         * Med regel som skal fylle ut verdier som ikke blir satt eksplisitt
         * Saksmappen blir ikke opprettet hvis den allerede eksisterer, men journalpost blir opprettet 
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Saksmappe_Og_Journalpost_I_En_Melding()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe og journalpost og send til arkiv
             * Med klassifikasjon
             */

            
            var mappe = MappeBuilder.Init().WithKlassifikasjon(GenererKlassifikasjon()).BuildSaksmappe(_saksmappeEksternNoekkel);
            var referanseTilSaksmappe = new ReferanseTilMappe()
            {
                ReferanseEksternNoekkel = _saksmappeEksternNoekkel
            };
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            // Legg til journalpost i arkivmelding
            var journalpost = JournalpostBuilder
                .Init()
                .WithTittel("Test tittel")
                .WithReferanseTilForelderMappe(referanseTilSaksmappe)
                .Build(
                    fagsystem: FagsystemNavn,
                    saksbehandlerNavn: SaksbehandlerNavn);
            
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            
            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;
            arkivmelding.Mappe = mappe;
            arkivmelding.Regel = ArkivmeldingRegel;

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            // File.WriteAllText("ArkivmeldingMedRegelNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized, null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var journalpostHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkelNyJournalpost).Build();
            
            var journalpostHentAsString = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får RegistreringHentResultat melding
            SjekkForventetMelding(journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent meldingen
            var registreringHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.IsNotNull(registreringHentResultatMelding);
            
            var registreringHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(registreringHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(registreringHentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(registreringHentResultatPayload.PayloadAsString);

            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
    }
}