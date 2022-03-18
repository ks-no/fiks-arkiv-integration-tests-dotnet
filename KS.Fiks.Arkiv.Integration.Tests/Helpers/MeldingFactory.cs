using System;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.EksternNoekkel;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers
{
    public class MeldingFactory
    {
        public static JournalpostHent CreateJournalpostHent(Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering.EksternNoekkel referanseEksternNoekkel)
        {
             return new JournalpostHent()
             {
                 ReferanseEksternNoekkel = new Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.EksternNoekkel()
                 {
                     Fagsystem = referanseEksternNoekkel.Fagsystem,
                     Noekkel = referanseEksternNoekkel.Noekkel
                 }
             };
        }
        
        public static JournalpostHent CreateJournalpostHent(SystemID systemId )
        {
            return new JournalpostHent()
            {
               SystemID = new SystemID()
               {
                   Label = systemId.Label,
                   Value = systemId.Value
               },
            };
        }

        public static Arkivmelding CreateNyJournalpostMelding(string referanseEksternNoekkelNoekkel = null)
         {
             var nyJournalpost = new Arkivmelding()
             {
                 System = "Fagsystem X",
                 MeldingId = Guid.NewGuid().ToString(),
                 AntallFiler = 1,
                 Registrering =
                 {
                     new Journalpost()
                     {
                         OpprettetAv = "En brukerid",
                         ArkivertAv = "En brukerid",
                         ReferanseForelderMappe = new SystemID() { Label = "", Value = Guid.NewGuid().ToString() },
                         Dokumentbeskrivelse =
                         {
                             new Dokumentbeskrivelse()
                             {
                                 Dokumenttype = "SØKNAD",
                                 Dokumentstatus = "F",
                                 Tittel = "Rekvisisjon av oppmålingsforretning",
                                 TilknyttetRegistreringSom = "H",
                                 Dokumentobjekt =
                                 {
                                     new Dokumentobjekt()
                                     {
                                         Versjonsnummer = "1",
                                         Variantformat = "P",
                                         Format = "PDF",
                                         Filnavn = "rekvisjon.pdf",
                                         ReferanseDokumentfil = "rekvisisjon.pdf"
                                     }
                                 }
                             }
                         },
                         Tittel = "Internt notat",
                         Korrespondansepart =
                         {
                             new Korrespondansepart()
                             {
                                 Korrespondanseparttype = "IM",
                                 KorrespondansepartNavn = "Oppmålingsetaten",
                                 AdministrativEnhet = "Oppmålingsetaten",
                                 Saksbehandler = "Ingrid Mottaker"
                             }
                         },
                         ReferanseEksternNoekkel = new Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel()
                         {
                             Fagsystem = "Fiks-arkiv integrasjonstest",
                             Noekkel = referanseEksternNoekkelNoekkel ?? Guid.NewGuid().ToString(),
                         },
                         Journalposttype = "X",
                         Journalstatus = "F",
                         DokumentetsDato = DateTime.Now.Date,
                         MottattDato = DateTime.Now,
                     },
                 }
             };
             return nyJournalpost;
         }
    }
}