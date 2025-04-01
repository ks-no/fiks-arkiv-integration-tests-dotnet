using System;
using System.Linq;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
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
         * Denne testen søker etter saksmapper vha matrikkel
         * og deretter henter saksmappe basert på resultat av søket
         */
        [Test, Order(1)]
        public async Task A_Soek_Etter_Eiendom()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();
            
            // Change these values for searching on matrikkel
            var gnr = 1;
            var bnr = 2;
            var snr = 3;
            var fnr = 4;
            var knr = 5;

            /*
             * STEG 1:
             * Søk etter saksmapper på matrikkel med responstype noekler (bare nøkler)
             */
            var sokMelding = SokBuilder.Init()
                .WithSaksmappeMatrikkelSok(gnr, bnr, snr, fnr, knr )
                .WithResponstype(Responstype.Noekler)
                .Build();

            var sokMeldingAsString = SerializeHelper.Serialize(sokMelding);
            
            // Valider innhold (xml)
            validator.Validate(sokMeldingAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();

            // Send sok-melding
            var sokMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.Sok, sokMeldingAsString, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får SokResultatNoekler melding
            SjekkForventetMelding(MottatMeldingArgsList, sokMeldingId, FiksArkivMeldingtype.SokResultatNoekler);
            
            // Hent meldingen
            var sokResultatMelding = GetMottattMelding(MottatMeldingArgsList, sokMeldingId, FiksArkivMeldingtype.SokResultatNoekler);

            Assert.That(sokResultatMelding != null);
            
            var sokResultatPayload = MeldingHelper.GetDecryptedMessagePayload(sokResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(sokResultatPayload.PayloadAsString);

            var sokResultat = SerializeHelper.DeserializeXml<SokeresultatNoekler>(sokResultatPayload.PayloadAsString);

            Assert.That(sokResultat.Count > 0);

            var _saksmappeEksternNoekkel = sokResultat.ResultatListe.First().Saksmappe.ReferanseEksternNoekkel;
            
            /*
             * STEG 2:
             * Hent første saksmappe fra søkeresultat og valider
             * TODO Denne kan utvides til å hente alle mapper som kom tilbake fra søket
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
            var hentSaksmappeResultatMelding = GetMottattMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.That(hentSaksmappeResultatMelding != null);
            
            var mappeHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(hentSaksmappeResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = SerializeHelper.DeserializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

        }
    }
}