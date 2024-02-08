using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Send.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
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
            var journalpostHent = RegistreringHentBuilder
                .Init()
                .WithEksternNoekkel(GenererEksternNoekkel(Guid.Empty.ToString())) // Denne burde ikke eksistere i systemet
                .Build();
            new TestHarness( this, FiksArkivMeldingtype.RegistreringHent, journalpostHent )
                .Send()
                .ExpectToFail(FiksArkivMeldingtype.Ikkefunnet);
        }
    }
}