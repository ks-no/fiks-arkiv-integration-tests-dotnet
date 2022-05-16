using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Arkivering
{
    /**
     * Disse testene sender først en arkivmelding for å opprette en saksmappe for så å sende inn en journalpost med dokument på saksmappen
     * å sende inn nytt dokument på journalpost
     */
    public class NySaksmappe : IntegrationTestsBase
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
        public void Opprett_Ny_Saksmappe_Deretter_Ny_Journalpost_Med_Hoveddokument()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            var referanseEksternNoekkelNySaksmappe = new EksternNoekkel()
            {
                Fagsystem = "Integrasjonstest for ny saksmappe og ny journalpost - saksmappen",
                Noekkel = Guid.NewGuid().ToString()
            };

            Console.Out.WriteLine(
                $"Sender arkivmelding med ny journalpost med EksternNoekkel fagsystem {referanseEksternNoekkelNySaksmappe.Fagsystem} og noekkel {referanseEksternNoekkelNySaksmappe.Noekkel} og dokument som vedlegg");
            
            // STEG 1: Opprett arkivmelding med en saksmappe og send inn
            var arkivmeldingNySaksmappe = MeldingGenerator.CreateArkivmeldingMedSaksmappe(referanseEksternNoekkelNySaksmappe);

            var nySaksmappeSerialized = ArkiveringSerializeHelper.Serialize(arkivmeldingNySaksmappe);
            var validator = new SimpleXsdValidator();
            
            // Valider arkivmelding
            validator.Validate(nySaksmappeSerialized);
            
            File.WriteAllText("ArkivmeldingMedNySaksmappe2.xml", nySaksmappeSerialized);

            // Send melding
            var nySaksmappeMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nySaksmappeSerialized, "arkivmelding.xml", null, testSessionId);
            
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
            
            var arkivmeldingKvittering = ArkiveringSerializeHelper.DeSerializeXml<ArkivmeldingKvittering>(arkivmeldingKvitteringPayload.PayloadAsString);
            
            var referanseEksternNoekkelNyJournalpost= new EksternNoekkel()
            {
                Fagsystem = "Integrasjonstest for ny saksmappe og ny journalpost - journalposten",
                Noekkel = Guid.NewGuid().ToString()
            };

            var referanseForelderMappe = new SystemID() { Value = arkivmeldingKvittering.MappeKvittering[0].SystemID.Value, Label = arkivmeldingKvittering.MappeKvittering[0].SystemID.Label};
            
            // STEG 2: Opprett arkivmelding med en journalpost og dokument og send inn til riktig forelder mappe
            var journalpost = JournalpostGenerator.CreateJournalpost(referanseForelderMappe);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            var arkivmelding = MeldingGenerator.CreateArkivmelding();
            arkivmelding.Registrering.Add(journalpost);

            var nyJournalpostSerialized = ArkiveringSerializeHelper.Serialize(arkivmelding);
            
            File.WriteAllText("ArkivmeldingMedNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            arkivmeldingKvitteringMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            // STEG 3: Henting av journalpost
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkelNyJournalpost);
            
            var journalpostHentAsString = ArkiveringSerializeHelper.Serialize(journalpostHent);
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();
            
            // Send hent melding
            var journalpostHentMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.JournalpostHent, journalpostHentAsString, "arkivmelding.xml", null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får journalpostHentResultat melding
            var journalpostHentResultatMelding = GetAndVerifyByMeldingstype(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = ArkiveringSerializeHelper.DeSerializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);

            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
    }
}