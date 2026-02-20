using System;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Brukstilfeller.Skjema
{
    /**
     * Disse testene er til eksempel for "skjemasystemer"
     * Motorferdsel sine eksempler er brukt som utgangsupnkt
     * ArkivmeldingRegel: Denne bør endres til en regel som passer til ditt behov for testing. Kan være forskjellig pr test!
     * OBS: Alle testene må kjøres og i riktig rekkefølge for at saksmappe skal først opprettes, og så legges det til journalposter
     */
    public class SkjemasystemTests : IntegrationTestsBase
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
            
            _saksansvarligInitialer = FagsystemNavn.ToUpper();
            
            // SaksmappeEksternNoekkelNoekkel er nøkkelen for saksmappen, hvis man ønsker å få opprettet ny så kan man utelate den som parameter og få generert helt ny.
            _saksmappeEksternNoekkel = GenererEksternNoekkel(SaksmappeEksternNoekkelNoekkel);
        }
        
        /*
         * Motorferdsel sitt eksempel er brukt i denne testen, mtp regel osv.
         * Denne testen oppretter saksmappe med skjerming
         * Til slutt henter testen opprettet saksmappe og validerer den
         * OBS: Denne oppretter mappen som brukes i testene som følger
         */
        [Test, Order(1)]
        public async Task A_Opprett_Saksmappe_Med_Skjerming()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            
            // Regel er ikke brukt for dette usecaset!
            // ArkivmeldingRegel = "NADA";

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe og send til arkiv
             */

            var mappe = MappeBuilder.Init()
                .WithTittel($"Test tittel")
                .WithOffentligTittel($"Test tittel - offentlg tittel!")
                .WithSkjerming(new Skjerming()
                {
                    Tilgangsrestriksjon = new Tilgangsrestriksjon()
                    {
                        KodeProperty = "KL"
                    },
                    Skjermingshjemmel = "Ofl. § 5a, fvl. § 13."
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"FH",
                    KlasseID = "8.3.D12",
                    Tittel = "Beskrivende tekst"
                })
                .WithKlassifikasjon(new Klassifikasjon()
                {
                    KlassifikasjonssystemID = $"GBN",
                    KlasseID = "20/19/458/0",
                    Tittel = "Thomas von Westens gate 13"
                })
                .WithAdministrativEnhet(new AdministrativEnhet()
                {
                    Navn = "Administrasjon",
                    Initialer = "ADM",
                })
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
            var nySaksmappeMeldingId = await FiksRequestService.SendAsync(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett, nySaksmappe, null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt og arkivmelding kvittering meldinger tilbake
            SjekkForventetMeldinger(MottatMeldingArgsList, nySaksmappeMeldingId, new []{FiksArkivMeldingtype.ArkivmeldingOpprettMottatt, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering});

            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);
            
            arkivmeldingKvitteringPayload = await MeldingHelper.GetDecryptedMessagePayloadAsync(arkivmeldingKvitteringMelding);
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 2:
             * Hent opprettet saksmappe igjen og valider
             * 
             */
            var mappeHent = MappeHentBuilder.Init().WithEksternNoekkel(_saksmappeEksternNoekkel).Build();
            
            var mappeHentAsString = SerializeHelper.Serialize(mappeHent);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var mappeHentMeldingId = await FiksRequestService.SendAsync(MottakerKontoId, FiksArkivMeldingtype.MappeHent, mappeHentAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får MappeHentResultat melding
            SjekkForventetMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);
            
            // Hent meldingen
            var mappeHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.That(mappeHentResultatMelding != null);
            
            var mappeHentResultatPayload = await MeldingHelper.GetDecryptedMessagePayloadAsync(mappeHentResultatMelding);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = SerializeHelper.DeserializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

            Assert.That(mappeHentResultat.Mappe.ReferanseEksternNoekkel.Fagsystem == arkivmelding.Mappe.ReferanseEksternNoekkel.Fagsystem);
            Assert.That(mappeHentResultat.Mappe.ReferanseEksternNoekkel.Noekkel == arkivmelding.Mappe.ReferanseEksternNoekkel.Noekkel);
        }
    }
}