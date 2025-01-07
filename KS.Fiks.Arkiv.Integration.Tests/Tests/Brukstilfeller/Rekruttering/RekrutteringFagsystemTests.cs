using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Brukstilfeller.Rekruttering
{
    /**
     * Disse testene er til eksempel for brukstilfelle for rekruttering: https://github.com/ks-no/fiks-arkiv-specification/wiki/Brukstilfelle-Rekruttering
     * EasyCruit sine eksempler er brukt som utgangsupnkt
     * ArkivmeldingRegel: Denne bør endres til en regel som passer til ditt behov for testing. Kan være forskjellig pr test!
     * OBS: Alle testene må kjøres og i riktig rekkefølge for at saksmappe skal først opprettes, og så legges det til journalposter
     */
    public class RekrutteringFagsystemTests : IntegrationTestsBase
    {
        // SaksmappeEksternNoekkelNoekkel er nøkkelen for saksmappen
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
        private static string ArkivmeldingRegel;
        private string _saksansvarligInitialer;
        private EksternNoekkel _saksmappeEksternNoekkel;

        [SetUp]
        public async Task Setup()
        {
            await Init();
            
            validator = new SimpleXsdValidator();
            _saksansvarligInitialer = FagsystemNavn.ToUpper();
            
            // SaksmappeEksternNoekkelNoekkel er nøkkelen for saksmappen, hvis man ønsker å få opprettet ny så kan man utelate den som parameter og få generert helt ny.
            _saksmappeEksternNoekkel = GenererEksternNoekkel(SaksmappeEksternNoekkelNoekkel);
        }
        
        /*
         * EasyCruit sitt eksempel er brukt i denne testen, mtp regel osv.
         * Denne testen oppretter saksmappe for en ledig stilling
         * Til slutt henter testen opprettet saksmappe og validerer den
         * OBS: Denne oppretter mappen som brukes i testene som følger
         */
        [Test, Order(1)]
        public async Task A_Opprett_Ledig_Stilling_Saksmappe()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            
            // Regel for dette usecaset!
            ArkivmeldingRegel = "VEC-VACANCY-CASE";

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe og send til arkiv
             */

            var mappe = MappeBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - Ledig stilling!")
                .WithOffentligTittel($"Test fra {FagsystemNavn} - Ledig stilling! - offentlig tittel")
                .BuildSaksmappe(_saksmappeEksternNoekkel);
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;


            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Mappe = mappe;

            var nySaksmappe = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            // File.WriteAllText("RekrutteringMedSaksmappeOgRegel.xml", nySaksmappe);
            
            // Valider arkivmelding
            validator.Validate(nySaksmappe);

            // Send melding
            var nySaksmappeMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nySaksmappe, null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nySaksmappeMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent oppprettet saksmappe igjen og valider
             * 
             */
            var mappeHent = MappeHentBuilder.Init().WithEksternNoekkel(_saksmappeEksternNoekkel).Build();
            
            var mappeHentAsString = SerializeHelper.Serialize(mappeHent);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var mappeHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.MappeHent, mappeHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får MappeHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);
            
            // Hent meldingen
            var mappeHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.That(mappeHentResultatMelding != null);
            
            var mappeHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(mappeHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = SerializeHelper.DeserializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

            Assert.That(mappeHentResultat.Mappe.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Mappe.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(mappeHentResultat.Mappe.ReferanseEksternNoekkel.Noekkel == arkivmelding.Mappe.ReferanseEksternNoekkel.Noekkel);
        }
    
          /*
         * EasyCruit sitt eksempel er brukt i denne testen, mtp regel osv.
         * Denne testen oppretter journalposten for en ledig stilling på saksmappe opprettet tidligere
         * Til slutt henter testen opprettet journalpost og validerer den
         */
        [Test, Order(2)]
        public async Task B_Opprett_Ledig_Stilling_Journalpost_Med_HovedDokument_På_Saksmappen()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            
            // Regel for dette usecaset!
            ArkivmeldingRegel = "VEC-PROJECT-DOCUMENT";

            /*
             * STEG 1:
             * Opprett arkivmelding med journalpost og send til arkiv
             */
            
            // Dokumentbeskrivelse med dokumentobjekt for hoveddokument
            var dokumentbeskrivelse = 
                DokumentbeskrivelseBuilder.Init()
                    .WithTittel("Prosjektdokument - ref-number-123")
                    .WithDokumenttype("DOKUMENT", "Dokument")
                    .WithDokumentstatus("F", "Dokumentet er ferdigstilt")
                    .WithTilknyttetRegistreringSom("H", "Hoveddokument")
                    .WithDokumentobjekt(new Dokumentobjekt()
                    {
                        Versjonsnummer = 1,
                        Variantformat = new Variantformat()
                        {
                            KodeProperty = "P",
                            Beskrivelse = "Produksjonsformat"
                        },
                        Format = new Format()
                        {
                            KodeProperty = "RA-PDF"
                        },
                        Filnavn = "hoveddokument-test-vedlegg.pdf",
                        ReferanseDokumentfil = "test-vedlegg.pdf"
                    })
                    .Build();
            
            var korrespondansepart = new Korrespondansepart();
            korrespondansepart.KorrespondansepartNavn = "Navn Navnesen";
            korrespondansepart.Epostadresse = "Navn.Navnesen@etellerannet.no";
            korrespondansepart.ErBehandlingsansvarlig = false;
            korrespondansepart.Korrespondanseparttype = new Korrespondanseparttype()
            {
                KodeProperty = "IA",
                Beskrivelse = "Intern avsender"
            };

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            var journalpost = JournalpostBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - Prosjektdokument - ref-number-123")
                .WithOffentligTittel($"Test fra {FagsystemNavn} - Prosjektdokument - ref-number-123")
                .WithEksternNoekkel(referanseEksternNoekkelNyJournalpost)
                .WithReferanseTilForelderMappe(new ReferanseTilMappe()
                {
                    ReferanseEksternNoekkel = _saksmappeEksternNoekkel // Refererer til saksmappen opprettet tidligere
                })
                .WithJournalposttype("X", "Organinternt dokument uten oppfølging")
                .WithJournalstatus("A")
                .WithDokumentbeskrivelse(dokumentbeskrivelse)
                .WithKorrespondansepart(korrespondansepart)
                .Build();

            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;


            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;

            var nyJournalpost = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            // File.WriteAllText("RekrutteringMedJournalpostDokumentOgRegel.xml", nyJournalpost);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpost);

            // Send melding
            var meldingAttachment = new MeldingAttachment
            {
                Filename = "test-vedlegg.pdf",
                Filestream = new FileStream("test-vedlegg.pdf", FileMode.Open, FileAccess.Read, FileShare.None)
            };
            var attachments = new List<MeldingAttachment> { meldingAttachment };
            var nySaksmappeMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpost, attachments, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nySaksmappeMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var registreringHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkelNyJournalpost).Build();
            
            var registreringHentAsString = SerializeHelper.Serialize(registreringHent);
            
            // Valider innhold (xml)
            validator.Validate(registreringHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var hentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, registreringHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får MappeHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, hentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent meldingen
            var hentResultatMelding = GetMottattMelding(MottatMeldingArgsList, hentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.That(hentResultatMelding != null);
            
            var hentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(hentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(hentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(hentResultatPayload.PayloadAsString);

            Assert.That(registreringHentResultat.Registrering.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(registreringHentResultat.Registrering.ReferanseEksternNoekkel.Noekkel == arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
        
        /*
         * EasyCruit sitt eksempel er brukt i denne testen, mtp regel osv.
         * Denne testen oppretter journalposten med innstillingsvedtak for en ledig stilling på saksmappe opprettet tidligere
         * Til slutt henter testen opprettet journalpost og validerer den
         */
        [Test, Order(3)]
        public async Task C_Opprett_Innstillingsvedtak_Journalpost_Med_HovedDokument_På_Saksmappen()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            
            // Regel for dette usecaset!
            ArkivmeldingRegel = "VEC-NOMINATION-LIST";

            /*
             * STEG 1:
             * Opprett arkivmelding med journalpost og send til arkiv
             */
            
            // Dokumentbeskrivelse med dokumentobjekt for hoveddokument
            var dokumentbeskrivelse = 
                DokumentbeskrivelseBuilder.Init()
                    .WithTittel("Innstillingsvedtak - ref-number-123")
                    .WithDokumenttype("VEDTAK", "Vedtak")
                    .WithDokumentstatus("F", "Dokumentet er ferdigstilt")
                    .WithTilknyttetRegistreringSom("H", "Hoveddokument")
                    .WithDokumentobjekt(new Dokumentobjekt()
                    {
                        Versjonsnummer = 1,
                        Variantformat = new Variantformat()
                        {
                            KodeProperty = "P",
                            Beskrivelse = "Produksjonsformat"
                        },
                        Format = new Format()
                        {
                            KodeProperty = "RA-PDF"
                        },
                        Filnavn = "hoveddokument-test-vedlegg.pdf",
                        ReferanseDokumentfil = "test-vedlegg.pdf"
                    })
                    .Build();
            
            var korrespondansepart = new Korrespondansepart();
            korrespondansepart.KorrespondansepartNavn = "Navn Navnesen";
            korrespondansepart.Epostadresse = "Navn.Navnesen@etellerannet.no";
            korrespondansepart.ErBehandlingsansvarlig = false;
            korrespondansepart.Korrespondanseparttype = new Korrespondanseparttype()
            {
                KodeProperty = "IA",
                Beskrivelse = "Intern avsender"
            };

            var referanseEksternNoekkelNyJournalpost = GenererEksternNoekkel();

            var journalpost = JournalpostBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - Innstillingsvedtak - ref-number-123")
                .WithOffentligTittel($"Test fra {FagsystemNavn} - Innstillingsvedtak - ref-number-123")
                .WithEksternNoekkel(referanseEksternNoekkelNyJournalpost)
                .WithReferanseTilForelderMappe(new ReferanseTilMappe()
                {
                    ReferanseEksternNoekkel = _saksmappeEksternNoekkel // Refererer til saksmappen opprettet tidligere
                })
                .WithJournalposttype("X", "Organinternt dokument uten oppfølging")
                .WithJournalstatus("A")
                .WithDokumentbeskrivelse(dokumentbeskrivelse)
                .WithKorrespondansepart(korrespondansepart)
                .Build();

            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;


            var arkivmelding = MeldingGenerator.CreateArkivmelding(FagsystemNavn);
            arkivmelding.Registrering = journalpost;

            var nyJournalpost = SerializeHelper.Serialize(arkivmelding);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            // File.WriteAllText("RekrutteringMedJournalpostDokumentOgRegel.xml", nyJournalpost);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpost);

            // Send melding
            var meldingAttachment = new MeldingAttachment
            {
                Filename = "test-vedlegg.pdf",
                Filestream = new FileStream("test-vedlegg.pdf", FileMode.Open, FileAccess.Read, FileShare.None)
            };
            var attachments = new List<MeldingAttachment> { meldingAttachment };
            var nySaksmappeMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nyJournalpost, attachments, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nySaksmappeMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var registreringHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkelNyJournalpost).Build();
            
            var registreringHentAsString = SerializeHelper.Serialize(registreringHent);
            
            // Valider innhold (xml)
            validator.Validate(registreringHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var hentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, registreringHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får MappeHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, hentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);
            
            // Hent meldingen
            var hentResultatMelding = GetMottattMelding(MottatMeldingArgsList, hentMeldingId, FiksArkivMeldingtype.RegistreringHentResultat);

            Assert.That(hentResultatMelding != null);
            
            var hentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(hentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(hentResultatPayload.PayloadAsString);

            var registreringHentResultat = SerializeHelper.DeserializeXml<RegistreringHentResultat>(hentResultatPayload.PayloadAsString);

            Assert.That(registreringHentResultat.Registrering.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Registrering.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(registreringHentResultat.Registrering.ReferanseEksternNoekkel.Noekkel == arkivmelding.Registrering.ReferanseEksternNoekkel.Noekkel);
        }
        
    }
}