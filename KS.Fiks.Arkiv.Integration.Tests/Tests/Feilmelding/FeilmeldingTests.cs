using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.Protokoller.V1.Models.Feilmelding;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.InnsynTests
{
    /**
     * Disse testene se forsøker å framprovosere feil og sjekke at man får korrekt feilmelding tilbake*Æ^`  
     */
    public class FeilmeldingTests : IntegrationTestsBase
    {
        [SetUp]
        public void Setup()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste. 
            _mottatMeldingArgsList = new List<MottattMeldingArgs>();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Local.json")
                .Build();
            _client = new FiksIOClient(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            _client.NewSubscription(OnMottattMelding);
            _fiksRequestService = new FiksRequestMessageService(config);
            _mottakerKontoId = Guid.Parse(config["TestConfig:ArkivAccountId"]);
        }

        [Test] public void Hent_Journalpost_Med_Ikke_Eksisterende_EksternNoekkel_Returnerer_Ikkefunnet()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers. 
            var testSessionId = Guid.NewGuid().ToString();
            var validator = new SimpleXsdValidator();
            
            var referanseEksternNoekkelIkkeGyldig = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest hent journalpost med ikke gyldig referanseEksternNoekkel",
                Noekkel = Guid.Empty.ToString() // Bør ikke kunne eksistere i Arkivet
            };
            
            // STEG 1: Forsøke hente journalpost som ikke eksisterer
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkelIkkeGyldig);
            
            var journalpostHentSerialized = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentSerialized);
            
            File.WriteAllText("HentJournalpostEksternNoekkelIkkeGyldig.xml", journalpostHentSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.JournalpostHent, journalpostHentSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får en Ikkefunnet feil-melding
            var ikkefunnetFeilmelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, FeilmeldingType.Ikkefunnet);

            Assert.IsNotNull(ikkefunnetFeilmelding);
            
            //TODO hent feilmelding og sjekk innhold?
        }
    }
}