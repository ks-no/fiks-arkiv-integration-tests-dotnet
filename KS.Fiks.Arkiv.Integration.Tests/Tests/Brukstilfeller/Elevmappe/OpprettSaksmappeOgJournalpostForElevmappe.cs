using System;
using System.Collections.Generic;
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

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Brukstilfeller.Elevmappe
{
    /**
     * Disse testene er til eksempel for brukstilfelle for elevmapper: https://github.com/ks-no/fiks-arkiv-specification/wiki/Brukstilfelle-Elevmappe
     * ArkivmeldingRegel: Denne bør endres til en regel som passer til ditt behov for testing
     */
    public class ElevmappeTests : IntegrationTestsBase
    {
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
        private const string ArkivmeldingRegel = "FiksArkiv-Elevmappe-1";
        private string _saksansvarligInitialer;
        private EksternNoekkel _saksmappeEksternNoekkel;

        [SetUp]
        public async Task Setup()
        {
            await Init();
            
            validator = new SimpleXsdValidator();

            _saksansvarligInitialer = FagsystemNavn.ToUpper();
            _saksmappeEksternNoekkel = GenererEksternNoekkel(SaksmappeEksternNoekkelNoekkel);
        }
        
        /*
         * Conexus sitt eksempel er brukt i denne testen, mtp klassifikasjoner osv.
         * Denne testen oppretter saksmappe hvis den ikke finnes og ny journalpost med et hoveddokument på saksmappen.
         * Saksmappen blir ikke opprettet hvis den allerede eksisterer, men journalpost blir opprettet 
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Elevmappe_Som_Saksmappe_Og_Journalpost_Med_Dokument_I_En_Melding()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe, med klassifikasjoner og journalpost og send til arkiv
             */

            var mappe = MappeBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - ny mappe med klasser")
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Stafettloggen",
                    KlasseID = "4.16",
                    Tittel = "Engage"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Loggtype",
                    KlasseID = "1",
                    Tittel = "Oppmerksomheter"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Institusjon",
                    KlasseID = "339272802",
                    Tittel = "Dummy test school 2"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Person",
                    KlasseID = "13061276334",
                    Tittel = "Eline Olavesen"
                })
                .WithAdministrativEnhet(new AdministrativEnhet(){ Initialer = "ADM" })
                .WithSaksansvarlig(new Saksansvarlig(){ Initialer = _saksansvarligInitialer})
                .BuildSaksmappe(_saksmappeEksternNoekkel);
            
            var referanseTilSaksmappe = new ReferanseTilMappe()
            {
                ReferanseEksternNoekkel = _saksmappeEksternNoekkel
            };
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            // Dokumentbeskrivelse med dokumentobjekt
            var dokumentbeskrivelse = 
                DokumentbeskrivelseBuilder.Init()
                    .WithTittel("Integrasjonstest leseprøve vår 2023")
                    .WithDokumenttype("SØKNAD")
                    .WithDokumentstatus("F")
                    .WithTilknyttetRegistreringSom("H")
                    .WithDokumentobjekt(new Dokumentobjekt()
                    {
                        Versjonsnummer = 1,
                        Variantformat = new Variantformat()
                        {
                            KodeProperty = "A"
                        },
                        Format = new Format()
                        {
                            KodeProperty = "PDF"
                        },
                        Filnavn = "rekvisisjon.pdf",
                        ReferanseDokumentfil = "rekvisisjon.pdf"
                    })
                    .Build();
            
            // Legg til journalpost i arkivmelding
            var journalpost = JournalpostBuilder
                .Init()
                .WithTittel("Integrasjonstest leseprøve resultat")
                .WithReferanseTilForelderMappe(referanseTilSaksmappe)
                .WithDokumentbeskrivelse(dokumentbeskrivelse)
                .WithJournalposttype("X")
                .WithJournalstatus("F")
                .Build(
                    fagsystem: FagsystemNavn,
                    saksbehandlerNavn: SaksbehandlerNavn);
            
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            
            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;
            arkivmelding.Mappe = mappe;

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            File.WriteAllText("ElevmappeArkivmeldingMedRegelNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var meldingAttachment = new MeldingAttachment
            {
                Filename = "rekvisisjon.pdf",
                Filestream = new FileStream("rekvisisjon.pdf", FileMode.Open, FileAccess.Read, FileShare.None)
            };
            var attachments = new List<MeldingAttachment> { meldingAttachment };
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized, attachments, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nyJournalpostMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
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

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får RegistreringHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent meldingen
            var registreringHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.That(registreringHentResultatMelding != null);
            
            var registreringHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(registreringHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(registreringHentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(registreringHentResultatPayload.PayloadAsString);

            Assert.That(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel == arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
    
        
        /*
         * Conexus sitt eksempel er brukt i denne testen, mtp klassifikasjoner osv.
         * Denne testen oppretter saksmappe hvis den ikke finnes og ny journalpost med et hoveddokument på saksmappen.
         * Med regel som skal fylle ut verdier som ikke blir satt eksplisitt
         * Saksmappen blir ikke opprettet hvis den allerede eksisterer, men journalpost blir opprettet 
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public async Task Opprett_Elevmappe_Som_Saksmappe_Og_Journalpost_Med_Dokument_I_En_Melding_Med_Regel()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe, med klassifikasjoner og journalpost og send til arkiv
             */

            var mappe = MappeBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - ny mappe med klasser")
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Stafettloggen",
                    KlasseID = "4.16",
                    Tittel = "Engage"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Loggtype",
                    KlasseID = "1",
                    Tittel = "Oppmerksomheter"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Institusjon",
                    KlasseID = "339272802",
                    Tittel = "Dummy test school 2"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"{FagsystemNavn} - Person",
                    KlasseID = "13061276334",
                    Tittel = "Eline Olavesen"
                })
                .WithAdministrativEnhet(new AdministrativEnhet(){ Initialer = "ADM" })
                .WithSaksansvarlig(new Saksansvarlig(){ Initialer = _saksansvarligInitialer})
                .BuildSaksmappe(_saksmappeEksternNoekkel);
            
            var referanseTilSaksmappe = new ReferanseTilMappe()
            {
                ReferanseEksternNoekkel = _saksmappeEksternNoekkel
            };
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            // Dokumentbeskrivelse med dokumentobjekt
            var dokumentbeskrivelse = 
                DokumentbeskrivelseBuilder.Init()
                    .WithTittel("Integrasjonstest leseprøve vår 2023")
                    .WithDokumenttype("SØKNAD")
                    .WithDokumentstatus("F")
                    .WithTilknyttetRegistreringSom("H")
                    .WithDokumentobjekt(new Dokumentobjekt()
                    {
                        Versjonsnummer = 1,
                        Variantformat = new Variantformat()
                        {
                            KodeProperty = "A"
                        },
                        Format = new Format()
                        {
                            KodeProperty = "PDF"
                        },
                        Filnavn = "rekvisisjon.pdf",
                        ReferanseDokumentfil = "rekvisisjon.pdf"
                    })
                    .Build();
            
            // Legg til journalpost i arkivmelding
            var journalpost = JournalpostBuilder
                .Init()
                .WithTittel("Integrasjonstest leseprøve resultat")
                .WithReferanseTilForelderMappe(referanseTilSaksmappe)
                .WithDokumentbeskrivelse(dokumentbeskrivelse)
                .WithJournalposttype("X")
                .WithJournalstatus("F")
                .Build(
                    fagsystem: FagsystemNavn,
                    saksbehandlerNavn: SaksbehandlerNavn);
            
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            
            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;
            arkivmelding.Mappe = mappe;
            
            //Setter regel
            arkivmelding.Regel = ArkivmeldingRegel;

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            // File.WriteAllText("ArkivmeldingMedRegelNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var meldingAttachment = new MeldingAttachment
            {
                Filename = "rekvisisjon.pdf",
                Filestream = new FileStream("rekvisisjon.pdf", FileMode.Open, FileAccess.Read, FileShare.None)
            };
            var attachments = new List<MeldingAttachment> { meldingAttachment };
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpostSerialized, attachments, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nyJournalpostMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
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

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får RegistreringHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent meldingen
            var registreringHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.That(registreringHentResultatMelding != null);
            
            var registreringHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(registreringHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(registreringHentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(registreringHentResultatPayload.PayloadAsString);

            Assert.That(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(registreringHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel == arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
    }
}