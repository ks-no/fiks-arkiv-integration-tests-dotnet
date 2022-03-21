using System;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering.EksternNoekkel;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MeldingGenerator
    {
        public static JournalpostHent CreateJournalpostHent(EksternNoekkel referanseEksternNoekkel)
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

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(string referanseEksternNoekkelNoekkel = null)
         {
             var arkivmelding = new Arkivmelding()
             {
                 System = "Fagsystem X",
                 MeldingId = Guid.NewGuid().ToString(),
                 AntallFiler = 1,
                 Registrering =
                 {
                     ArkivmeldingDataGenerator.CreateJournalpost(),
                 }
             };
             
             return arkivmelding;
         }

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = ArkivmeldingDataGenerator.CreateJournalpost();
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel;
            
            var arkivmelding = new Arkivmelding()
            {
                System = "Fagsystem X",
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
                Registrering =
                {
                    journalpost
                }
            };
             
            return arkivmelding;
        }

        public static JournalpostHent CreateJournalpostHent(IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel referanseEksternNoekkel)
        {
            return new JournalpostHent()
            {
                ReferanseEksternNoekkel = new IO.Arkiv.Client.Models.Innsyn.Hent.EksternNoekkel()
                {
                    Fagsystem = referanseEksternNoekkel.Fagsystem,
                    Noekkel = referanseEksternNoekkel.Noekkel
                }
            };
        }
    }
}