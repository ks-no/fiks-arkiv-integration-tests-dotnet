using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Sok
{
    /**
     * Disse testene sender først en arkivmelding for å opprette flere journalposter for så
     * søke etter journalpost
     */
    public class SokJournalpostTests : IntegrationTestsBase
    {
        [SetUp]
        public async Task Setup()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste. 
            MottatMeldingArgsList = new List<MottattMeldingArgs>();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Local.json")
                .Build();
            Client = await FiksIOClient.CreateAsync(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            Client.NewSubscription(OnMottattMelding);
            FiksRequestService = new FiksRequestMessageService(config);
            MottakerKontoId = Guid.Parse(config["TestConfig:ArkivAccountId"]);
            validator = new SimpleXsdValidator();
        }

        [Test]
        public async Task Sok_Etter_Journalpost_Med_Tittel_Og_Wildcard()
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

            var arkivmelding1 = await ArkiverJournalpost(testSessionId, tittel1);
            var arkivmelding2 = await ArkiverJournalpost(testSessionId, tittel2);
            var arkivmelding3 = await ArkiverJournalpost(testSessionId, tittel3);
            var arkivmelding4 = await ArkiverJournalpost(testSessionId, tittel4);

            /*
             * STEG 2:
             * Søk etter journalpost med tittel
             */

            var sokeord = "Tittel*";

            var sok = new Models.V1.Innsyn.Sok.Sok
            {
                Sokdefinisjon = new JournalpostSokdefinisjon()
                {
                    Inkluder = { JournalpostInkluder.Korrespondansepart },
                    Parametere = { 
                        new JournalpostParameter()
                        {
                            Felt = JournalpostSokefelt.RegistreringTittel,
                            Operator = OperatorType.Equal,
                            SokVerdier = new SokVerdier()
                            {
                                Stringvalues = { "En journalpost tittel med wildcard*" }
                            }
                        } 
                    },
                    Sortering =
                    {
                        new JournalpostSortering()
                        {
                            Felt = JournalpostSorteringsfelt.RegistreringOpprettetDato
                        }
                    },
                    Responstype = Responstype.Utvidet,
                },
                Take = 10,
                System = "Integrasjonstester",
            };

            // Send søk melding
            var sokSerialized = SerializeHelper.Serialize(sok);
            var sokMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.Sok,
                sokSerialized, "sok.xml", null, testSessionId);
            
            // Vent på respons
            VentPaSvar(1, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");
            
            // Verifiser at man får mottatt melding
            SjekkForventetMelding(MottatMeldingArgsList, sokMeldingId, FiksArkivMeldingtype.SokResultatUtvidet);

            // Hent melding
            var sokeresultatUtvidetMelding = GetMottattMelding(MottatMeldingArgsList, sokMeldingId,
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

        private async Task<Arkivmelding> ArkiverJournalpost(string testSessionId, string tittel)
        {
            var arkivmelding = MeldingGenerator.CreateArkivmeldingMedNyJournalpost(tittel: tittel);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            var validator = new SimpleXsdValidator();

            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNyJournalpost.xml", nyJournalpostSerialized);

            // Send arkiver melding
            var nyJournalpostMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett,
                nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);

            Console.Out.WriteLineAsync($"Arkivmelding med ny journalpost med tittel {tittel} sendt");

            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(MottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);

            // Hent melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nyJournalpostMeldingId,
                FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);

            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml",
                "Filnavn ikke som forventet arkivmelding-kvittering.xml");

            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();

            var arkivmeldingKvittering =
                SerializeHelper.DeserializeArkivmeldingKvittering(arkivmeldingKvitteringPayload.PayloadAsString);
            return arkivmelding;
        }
    }
}