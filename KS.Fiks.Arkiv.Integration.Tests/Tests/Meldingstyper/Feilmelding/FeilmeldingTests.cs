using System;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Feilmelding
{
    /**
     * Disse testene se forsøker å framprovosere feil og sjekke at man får korrekt feilmelding tilbake*Æ^`  
     */
    public class FeilmeldingTests : IntegrationTestsBase
    {
        [SetUp]
        public async Task Setup()
        {
            await Init();
        }

        [Test] 
        public async Task Hent_Journalpost_Med_Ikke_Eksisterende_EksternNoekkel_Returnerer_Ikkefunnet()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers. 
            var testSessionId = Guid.NewGuid().ToString();
            var validator = new SimpleXsdValidator();
            
            var referanseEksternNoekkelIkkeGyldig = new EksternNoekkel()
            {
                Fagsystem = FagsystemNavn,
                Noekkel = Guid.Empty.ToString() // Bør ikke kunne eksistere i Arkivet
            };
            
            /*
             * STEG 1:
             * Forsøk å hente journalpost som ikke skal eksistere
             */
            var journalpostHent = RegistreringHentBuilder.Init().WithEksternNoekkel(referanseEksternNoekkelIkkeGyldig).Build();
            
            var journalpostHentSerialized = SerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("HentJournalpostEksternNoekkelIkkeGyldig.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = await FiksRequestService.SendAsync(MottakerKontoId, FiksArkivMeldingtype.RegistreringHent, journalpostHentSerialized, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får en Ikkefunnet feil-melding
            var ikkefunnetFeilmelding = GetMottattMelding(MottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.Ikkefunnet);

            Assert.That(ikkefunnetFeilmelding != null);
        }
    }
}