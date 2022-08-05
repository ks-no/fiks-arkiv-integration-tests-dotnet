using System;
using System.Collections.Generic;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.EksternNoekkel;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Sok
{
    /**
     * Disse testene sender først en arkivmelding for å opprette flere journalposter for så
     * søke etter journalpost
     */
    public class SokJournalpostTests : IntegrationTestsBase
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
            validator = new SimpleXsdValidator();
        }
        
        [Test]
        public void Sok_Etter_Journalpost_Med_Tittel_Og_Wildcard()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers. 
            var testSessionId = Guid.NewGuid().ToString();
            
            /*
             * STEG 1:
             * Opprett arkivmeldinger med journalpost med forskjellige titler og send inn til arkiv
             */


            var tittel1 = "Tittel 1";
            var tittel2 = "Denne skal vi ikke få tilbake da den ikke inneholder søkeordet";
            var tittel3 = "Med en tittel til som kan bli til treff";
            var tittel4 = "Titteltei er et ord som inneholder søkeordet";

            var arkivmelding1 = ArkiverJournalpost(testSessionId, tittel1);
            var arkivmelding2 = ArkiverJournalpost(testSessionId, tittel2);
            var arkivmelding3 = ArkiverJournalpost(testSessionId, tittel3);
            var arkivmelding4 = ArkiverJournalpost(testSessionId, tittel4);
            
            /*
             * STEG 2:
             * Søk etter journalpost med tittel
             */

            var sokeord = "Tittel*";
            
            var sok = new Models.V1.Innsyn.Sok.Sok();
            var parameter = new Parameter()
            {
                Felt = SokFelt.RegistreringTittel,
                Operator = OperatorType.Equal,
                Parameterverdier = new Parameterverdier()
                {
                    Stringvalues = { sokeord }
                }
            };
            sok.Parameter.Add(parameter);
            sok.ResponsType = ResponsType.Utvidet;
            sok.Respons = Respons.Journalpost;
            sok.Take = 10;
            sok.System = "Integrasjonstester";
            sok.Tidspunkt = DateTime.Now;
            sok.MeldingId = Guid.NewGuid().ToString();

            // Send søk melding
            var sokSerialized = SerializeHelper.Serialize(sok);
            var sokMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Sok,
                sokSerialized, "sok.xml", null, testSessionId);
            
            // Vent på respons
            VentPaSvar(1, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får mottatt melding
            SjekkForventetMelding(_mottatMeldingArgsList, sokMeldingId, FiksArkivMeldingtype.SokResultatUtvidet);

            // Hent melding
            var sokeresultatUtvidetMelding = GetMottattMelding(_mottatMeldingArgsList, sokMeldingId,
                FiksArkivMeldingtype.SokResultatUtvidet);
            
            var payload = MeldingHelper.GetDecryptedMessagePayload(sokeresultatUtvidetMelding).Result;
            
            
            // Valider innhold (xml)
            validator.Validate(payload.PayloadAsString);
            
            var sokeresultatUtvidet =
                SerializeHelper.DeserializeSokeresultatUtvidet(payload.PayloadAsString);
            
            Assert.That(sokeresultatUtvidet.Count == 3);

            foreach (var resultat in sokeresultatUtvidet.ResultatListe)
            {
                Assert.That(resultat.Journalpost.Tittel.Equals(tittel1) || resultat.Journalpost.Tittel.Equals(tittel3) || resultat.Journalpost.Tittel.Equals(tittel4)); 
            }
        }

        private Arkivmelding ArkiverJournalpost(string testSessionId, string tittel)
        {
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(tittel: tittel);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            var validator = new SimpleXsdValidator();

            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            File.WriteAllText("ArkivmeldingMedNyJournalpost.xml", nyJournalpostSerialized);

            // Send arkiver melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding,
                nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);

            Console.Out.WriteLineAsync($"Arkivmelding med ny journalpost med tittel {tittel} sendt");

            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);

            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(_mottatMeldingArgsList, nyJournalpostMeldingId,
                FiksArkivMeldingtype.ArkivmeldingKvittering);

            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml",
                "Filnavn ikke som forventet arkivmelding-kvittering.xml");

            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            // Nullstill meldingsliste
            _mottatMeldingArgsList.Clear();

            var arkivmeldingKvittering =
                SerializeHelper.DeserializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            return arkivmelding;
        }
    }
}