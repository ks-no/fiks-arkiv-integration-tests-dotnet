using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.EksternNoekkel;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MeldingGenerator
    {
        public static JournalpostHent CreateJournalpostHent(EksternNoekkel referanseEksternNoekkel)
        {
             return new JournalpostHent()
             {
                 ReferanseEksternNoekkel = new Models.V1.Innsyn.Hent.Journalpost.EksternNoekkel()
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

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(EksternNoekkel referanseEksternNoekkelNoekkel)
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

        public static JournalpostHent CreateJournalpostHent(Models.V1.Arkivstruktur.EksternNoekkel referanseEksternNoekkel)
        {
            return new JournalpostHent()
            {
                ReferanseEksternNoekkel = new Models.V1.Innsyn.Hent.Journalpost.EksternNoekkel()
                {
                    Fagsystem = referanseEksternNoekkel.Fagsystem,
                    Noekkel = referanseEksternNoekkel.Noekkel
                }
            };
        }

        public static ArkivmeldingOppdatering CreateArkivmeldingOppdatering(EksternNoekkel referanseEksternNoekkel, string nyTittel)
        {
            return new ArkivmeldingOppdatering()
            {
                MeldingId = Guid.NewGuid().ToString(),
                Tidspunkt = DateTime.Now,
                RegistreringOppdateringer =
                {
                    new RegistreringOppdatering()
                    {
                        Tittel = nyTittel,
                        ReferanseEksternNoekkel = referanseEksternNoekkel
                    }
                }
            };
        }
    }
}