using System;
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
    public class OpprettSaksmappeOgJournalpostTests : IntegrationTestsBase
    {
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
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
         * Saksmappen blir ikke opprettet hvis den allerede eksisterer, men journalpost blir opprettet 
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Saksmappe_Og_Journalpost_I_En_Melding()
        {
            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe og journalpost og send til arkiv
             * Med klassifikasjon
             */
            var referanseTilSaksmappe = GenererReferanseTilMappe();
            var arkivHarness = MeldingGenerator.CreateArkivmelding(
                FagsystemNavn, 
                JournalpostBuilder
                    .Init()
                    .WithTittel("Test tittel")
                    .WithReferanseTilForelderMappe(referanseTilSaksmappe)
                    .WithReferanseEksternNoekkel(GenererEksternNoekkel())
                    .Build(
                        fagsystem: FagsystemNavn,
                        saksbehandlerNavn: SaksbehandlerNavn
                        ),
                MappeBuilder
                    .Init()
                    .WithKlassifikasjon(GenererKlassifikasjon())
                    .BuildSaksmappe(referanseTilSaksmappe.ReferanseEksternNoekkel)
                );
            var archiveCreate =new TestHarness(this, FiksArkivMeldingtype.ArkivmeldingOpprett, arkivHarness)
                .ExpectReceiveAllOfTypes(
                    FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, 
                    FiksArkivMeldingtype.ArkivmeldingOpprettKvittering)
                ;
            
            
            
            var (_, arkivmeldingKvitteringPayload) = archiveCreate.GetMelding( FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var hentHarness = new TestHarness(
                        this, 
                        FiksArkivMeldingtype.RegistreringHent, 
                        RegistreringHentBuilder
                            .Init()
                            .WithEksternNoekkel(arkivHarness.Registrering.ReferanseEksternNoekkel)
                            .Build()
                        )
                    .ExpectReturnOfType(FiksArkivMeldingtype.RegistreringHentResultat)
                ;
            var (registreringHentResultat, _, _) = hentHarness.GetMelding<RegistreringHentResultat>( FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivHarness.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivHarness.Registrering.ReferanseEksternNoekkel.Noekkel);
        }

        /*
         * Denne testen sjekker om en saksmappe finnes og hvis ikke oppretter den saksmappen med referanseEksternNoekkel
         * Så sender den inn en ny journalpost med et hoveddokument på saksmappen
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Eller_Hent_Saksmappe_Deretter_Opprett_Ny_Journalpost_Med_Hoveddokument_Verifiser_Hentet_Journalpost()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Sjekk om saksmappe eksisterer eller opprett arkivmelding med en saksmappe og send inn.
             * 
             */

            var saksmappeSystemID = await OpprettEllerHentSaksmappe(testSessionId);

            /*
             * STEG 2:
             * Opprett arkivmelding med en journalpost og dokument og send inn til riktig forelder mappe vha referanseForelderMappe.
             * referanseForeldermappe med SystemID fra saksmappe
             * 
             */
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            var referanseForelderMappe = new ReferanseTilMappe()
            {
                SystemID = saksmappeSystemID
            };

            var journalpost = JournalpostBuilder
                .Init()
                .WithTittel("Test")
                .WithReferanseTilForelderMappe(referanseForelderMappe)
                .Build(
                    fagsystem: FagsystemNavn,
                    saksbehandlerNavn: SaksbehandlerNavn
                    );
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
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
             * STEG 3:
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
        
        /*
         * Denne testen sjekker om en saksmappe finnes og hvis ikke oppretter den saksmappen med referanseEksternNoekkel
         * Så sender den inn en ny journalpost med et hoveddokument på saksmappen
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Eller_Hent_Saksmappe_Deretter_Opprett_Ny_Journalpost_Med_Hoveddokument_Verifiser_Hentet_Saksmappe()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Sjekk om saksmappe eksisterer eller opprett arkivmelding med en saksmappe og send inn.
             * 
             */

            var saksmappeSystemID = await OpprettEllerHentSaksmappe(testSessionId);

            /*
             * STEG 2:
             * Opprett arkivmelding med en journalpost og dokument og send inn til riktig forelder mappe vha referanseForelderMappe.
             * referanseForeldermappe med EksternNoekkel fra saksmappe
             * 
             */
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            var referanseForelderMappe = new ReferanseTilMappe()
            {
                ReferanseEksternNoekkel = _saksmappeEksternNoekkel
            };

            var journalpost = JournalpostBuilder.Init()
                .WithTittel("Test tittel")
                .WithReferanseTilForelderMappe(referanseForelderMappe)
                .Build(
                    fagsystem: FagsystemNavn,
                    saksbehandlerNavn: SaksbehandlerNavn
                    );
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
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
            
            // Hent melding
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 3:
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
            
            // Hent melding
            var registreringHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.IsNotNull(registreringHentResultatMelding);
            
            var registreringHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(registreringHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(registreringHentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(registreringHentResultatPayload.PayloadAsString);

            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }

        private async Task<SystemID> OpprettEllerHentSaksmappe(string testSessionId)
        {
            Console.Out.WriteLine(
                $"Forsøker hente saksmappe med referanseEksternNoekkel Fagsystem {FagsystemNavn} og Noekkel {SaksmappeEksternNoekkelNoekkel}");

            var saksmappe = await HentSaksmappe(testSessionId, _saksmappeEksternNoekkel);

            SystemID saksmappeSystemId;

            if (saksmappe == null)
            {
                Console.Out.WriteLine(
                    $"Fant ikke noen saksmappe med referanseEksternNoekkel Fagsystem {FagsystemNavn} og Noekkel {SaksmappeEksternNoekkelNoekkel}. Oppretter ny i stedet.");
                var arkivmeldingKvittering = await OpprettSaksmappe(testSessionId, _saksmappeEksternNoekkel);
                saksmappeSystemId = new SystemID()
                {
                    Value = arkivmeldingKvittering.MappeKvittering.SystemID.Value,
                    Label = arkivmeldingKvittering.MappeKvittering.SystemID.Label
                };
            }
            else
            {
                saksmappeSystemId = saksmappe.SystemID;
            }

            return saksmappeSystemId;
        }
    }
}