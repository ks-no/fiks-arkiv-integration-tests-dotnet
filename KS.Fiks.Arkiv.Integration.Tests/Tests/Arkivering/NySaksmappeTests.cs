using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.EksternNoekkel;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Saksmappe;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests.Arkivering
{
    /**
     * Disse testene sender først en arkivmelding for å hente en saksmappe og eventuelt opprette saksmappen hvis den ikke eksisterer.
     * For så å sende inn journalposter og eventuelt dokumenter på saksmappen
     */
    public class NySaksmappTests : IntegrationTestsBase
    {
        private const string EksternNoekkelFagsystem = "Validatortester saksmappe";
        private const string SaksmappeEksternNoekkelNoekkel = "4950bac7-79f2-4ec4-90bf-0c41e8d9ce78";
        private EksternNoekkel _saksmappeEksternNoekkel;

        [SetUp]
        public void Setup()
        {
            Init();
            
            validator = new SimpleXsdValidator();
            
            _saksmappeEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = EksternNoekkelFagsystem,
                Noekkel = SaksmappeEksternNoekkelNoekkel
            };
        }

        /*
         * Denne testen sjekker om en saksmappe finnes og hvis ikke oppretter den saksmappen med referanseEksternNoekkel
         * Så sender den inn en ny journalpost med et hoveddokument på saksmappen
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public void Opprett_Eller_Hent_Saksmappe_Deretter_Opprett_Ny_Journalpost_Med_Hoveddokument_Verifiser_Hentet_Journalpost()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Sjekk om saksmappe eksisterer eller opprett arkivmelding med en saksmappe og send inn.
             * 
             */

            var saksmappeSystemID = OpprettEllerHentSaksmappe(testSessionId);

            /*
             * STEG 2:
             * Opprett arkivmelding med en journalpost og dokument og send inn til riktig forelder mappe vha referanseForelderMappe.
             * referanseForeldermappe med SystemID fra saksmappe
             * 
             */
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;
            
            var referanseEksternNoekkelNyJournalpost= new EksternNoekkel()
            {
                Fagsystem = EksternNoekkelFagsystem,
                Noekkel = Guid.NewGuid().ToString()
            };

            var referanseForelderMappe = new ReferanseForelderMappe
            {
                SystemID = saksmappeSystemID
            };

            var journalpost = JournalpostGenerator.CreateJournalpost(referanseForelderMappe);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            var arkivmelding = MeldingGenerator.CreateArkivmelding();
            arkivmelding.Registrering.Add(journalpost);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            File.WriteAllText("ArkivmeldingMedNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            // Hent meldingen
            arkivmeldingKvitteringMelding = GetMottattMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 3:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkelNyJournalpost);
            
            var journalpostHentAsString = SerializeHelper.Serialize(journalpostHent);
            
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
            SjekkForventetMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.JournalpostHentResultat);
            
            // Hent meldingen
            var journalpostHentResultatMelding = GetMottattMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);

            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }
        
        /*
         * Denne testen sjekker om en saksmappe finnes og hvis ikke oppretter den saksmappen med referanseEksternNoekkel
         * Så sender den inn en ny journalpost med et hoveddokument på saksmappen
         * Journalpost bruker referanseForeldermappe med SystemID fra saksmappen
         * Til slutt henter testen journalpost og validerer den
         */
        [Test]
        public void Opprett_Eller_Hent_Saksmappe_Deretter_Opprett_Ny_Journalpost_Med_Hoveddokument_Verifiser_Hentet_Saksmappe()
        {
            // Denne id'en gjør at Arkiv-simulatoren ser hvilke meldinger som henger sammen. Har ingen funksjon ellers.  
            var testSessionId = Guid.NewGuid().ToString();

            /*
             * STEG 1:
             * Sjekk om saksmappe eksisterer eller opprett arkivmelding med en saksmappe og send inn.
             * 
             */

            var saksmappeSystemID = OpprettEllerHentSaksmappe(testSessionId);

            /*
             * STEG 2:
             * Opprett arkivmelding med en journalpost og dokument og send inn til riktig forelder mappe vha referanseForelderMappe.
             * referanseForeldermappe med EksternNoekkel fra saksmappe
             * 
             */
            
            MottattMeldingArgs? arkivmeldingKvitteringMelding;
            PayloadFile arkivmeldingKvitteringPayload;
            
            var referanseEksternNoekkelNyJournalpost= new EksternNoekkel()
            {
                Fagsystem = EksternNoekkelFagsystem,
                Noekkel = Guid.NewGuid().ToString()
            };

            var referanseForelderMappe = new ReferanseForelderMappe
            {
                ReferanseEksternNoekkel = _saksmappeEksternNoekkel
            };

            var journalpost = JournalpostGenerator.CreateJournalpost(referanseForelderMappe);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNyJournalpost;
            var arkivmelding = MeldingGenerator.CreateArkivmelding();
            arkivmelding.Registrering.Add(journalpost);

            var nyJournalpostSerialized = SerializeHelper.Serialize(arkivmelding);
            
            File.WriteAllText("ArkivmeldingMedNyJournalpostOgDokument.xml", nyJournalpostSerialized);
            
            // Valider arkivmelding
            validator.Validate(nyJournalpostSerialized);

            // Send melding
            var nyJournalpostMeldingId = _fiksRequestService.Send(_mottakerKontoId, FiksArkivMeldingtype.Arkivmelding, nyJournalpostSerialized, "arkivmelding.xml", null, testSessionId);
            
            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(_mottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            SjekkForventetMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            // Hent melding
            arkivmeldingKvitteringMelding = GetMottattMelding(_mottatMeldingArgsList, nyJournalpostMeldingId, FiksArkivMeldingtype.ArkivmeldingKvittering);
            
            arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml", "Filnavn ikke som forventet arkivmelding-kvittering.xml");
    
            // Valider innhold (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            /*
             * STEG 3:
             * Hent oppprettet journalpost igjen og valider
             * 
             */
            var journalpostHent = MeldingGenerator.CreateJournalpostHent(referanseEksternNoekkelNyJournalpost);
            
            var journalpostHentAsString = SerializeHelper.Serialize(journalpostHent);
            
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
            SjekkForventetMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.JournalpostHentResultat);
            
            // Hent melding
            var journalpostHentResultatMelding = GetMottattMelding(_mottatMeldingArgsList, journalpostHentMeldingId, FiksArkivMeldingtype.JournalpostHentResultat);

            Assert.IsNotNull(journalpostHentResultatMelding);
            
            var journalpostHentResultatPayload = MeldingHelper.GetDecryptedMessagePayload(journalpostHentResultatMelding).Result;
            
            // Valider innhold (xml)
            validator.Validate(journalpostHentResultatPayload.PayloadAsString);

            var journalpostHentResultat = SerializeHelper.DeserializeXml<JournalpostHentResultat>(journalpostHentResultatPayload.PayloadAsString);

            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Fagsystem, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem);
            Assert.AreEqual(journalpostHentResultat.Journalpost.ReferanseEksternNoekkel.Noekkel, arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel);
        }

        private SystemID OpprettEllerHentSaksmappe(string testSessionId)
        {
            Console.Out.WriteLine(
                $"Forsøker hente saksmappe med referanseEksternNoekkel Fagsystem {EksternNoekkelFagsystem} og Noekkel {SaksmappeEksternNoekkelNoekkel}");

            var saksmappe = HentSaksmappe(testSessionId, _saksmappeEksternNoekkel);

            SystemID saksmappeSystemID;

            if (saksmappe == null)
            {
                Console.Out.WriteLine(
                    $"Fant ikke noen saksmappe med referanseEksternNoekkel Fagsystem {EksternNoekkelFagsystem} og Noekkel {SaksmappeEksternNoekkelNoekkel}. Oppretter ny i stedet.");
                var arkivmeldingKvittering = OpprettSaksmappe(testSessionId, _saksmappeEksternNoekkel);
                saksmappeSystemID = new SystemID()
                {
                    Value = arkivmeldingKvittering.MappeKvittering[0].SystemID.Value,
                    Label = arkivmeldingKvittering.MappeKvittering[0].SystemID.Label
                };
            }
            else
            {
                saksmappeSystemID = saksmappe.SystemID;
            }

            return saksmappeSystemID;
        }
    }
}