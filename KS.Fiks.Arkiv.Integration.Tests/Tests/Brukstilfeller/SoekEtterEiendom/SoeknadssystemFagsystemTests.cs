using System;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Brukstilfeller.SoekEtterEiendom
{
    /**
     * Disse testene er til eksempel for brukstilfelle for søk etter eiendom: https://github.com/ks-no/fiks-arkiv-specification/wiki/Brukstilfelle-S%C3%B8knadssystem-med-s%C3%B8knadsfrist
     */
    public class SoekEtterEiendomTests : IntegrationTestsBase
    {
        // SaksmappeEksternNoekkelNoekkel er nøkkelen for saksmappen
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
        [SetUp]
        public async Task Setup()
        {
            await Init();
            
            validator = new SimpleXsdValidator();
        }
        
        /*
         * OBS: Denne testen søker etter eiendom
         */
        [Test, Order(1)]
        public async Task A_Soek_Etter_Eiendom()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            

            /*
             * STEG 1:
             * Opprett arkivmelding med saksmappe og send til arkiv
             */

            var mappe = MappeBuilder.Init()
                .WithTittel($"Test fra {FagsystemNavn} - Ledig stilling!")
                .WithOffentligTittel($"Test fra {FagsystemNavn} - Ledig stilling!")
                .BuildSaksmappe("");
            
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
    
    }
}