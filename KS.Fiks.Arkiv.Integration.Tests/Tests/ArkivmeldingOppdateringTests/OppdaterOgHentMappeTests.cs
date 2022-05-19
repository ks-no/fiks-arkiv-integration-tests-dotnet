using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.ArkivmeldingOppdateringTests
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en mappe (evt med journalpost i mappe) for så
     * oppdatere mappen ved oppdater-meldinger, og til slutt hente mappen igjen vha enten referanseEksternNoekkel eller systemID
     * for å bekrefte at mappe er endret som ønsket
     */
    public class OppdaterOgHentMappeTests : IntegrationTestsBase
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

        [Test]
        public void Oppdater_Saksmappe_Ansvarlig()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = "Fiks arkiv integrasjonstest for arkivmeldingoppdatering på saksmappe med eksternnoekkel",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny saksmappe med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");
            
            // STEG 1: Opprett arkivmelding med en saksmappe og send inn
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedSaksmappe(referanseEksternNoekkel);

            var nySaksmappeAsSerialized = ArkiveringSerializeHelper.Serialize(arkivmelding);
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nySaksmappeAsSerialized);

            // Send melding
            var nySaksmappeMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nySaksmappeAsSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            // STEG 2: Send oppdatering av saksmappe og saksansvarlig
            string nySaksansvarlig = "Nelly Ny Saksansvarlig";
            var arkivmeldingOppdatering = MeldingGenerator.CreateArkivmeldingOppdateringSaksmappeOppdateringNySaksansvarlig(referanseEksternNoekkel, nySaksansvarlig);
            
            var arkivmeldingOppdateringSerialized = ArkiveringSerializeHelper.Serialize(arkivmeldingOppdatering);
            
            // Valider innhold (xml)
            validator.Validate(arkivmeldingOppdateringSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send oppdater melding
            var arkivmeldingOppdaterMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOppdater, arkivmeldingOppdateringSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 2 respons meldinger. Mottat og kvittering 
            VentPaSvar(2, 10); 
            
            // Verifiser at man får mottatt melding
            GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får arkivmeldingOppdaterKvittering melding
            var arkivmeldingOppdaterKvittering = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, arkivmeldingOppdaterMeldingId, FiksArkivMeldingtype.ArkivmeldingOppdaterKvittering);

            Assert.IsNotNull(arkivmeldingOppdaterKvittering);
            
            // STEG 3: Henting av saksmappe
            var mappeHent = MeldingGenerator.CreateMappeHent(referanseEksternNoekkel);
            
            var mappeHentSerialized = ArkiveringSerializeHelper.Serialize(mappeHent);
            
            File.WriteAllText("MappeHent.xml", mappeHentSerialized);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentSerialized);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var mappeHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.MappeHent, mappeHentSerialized, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får mappeHentResultat melding
            var mappeHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.IsNotNull(mappeHentResultatMelding);
            
            var mappeHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(mappeHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = ArkiveringSerializeHelper.DeSerializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

            Saksmappe saksmappe = (Saksmappe)mappeHentResultat.Mappe;

            Assert.AreEqual(nySaksansvarlig, saksmappe.Saksansvarlig);
           // Assert.AreEqual(saksmappe.Ek.Fagsystem, arkivmelding.Mappe[0].ReferanseEksternNoekkel.Fagsystem);
            //Assert.AreEqual(mappeHentResultat.Mappe.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
    }
}