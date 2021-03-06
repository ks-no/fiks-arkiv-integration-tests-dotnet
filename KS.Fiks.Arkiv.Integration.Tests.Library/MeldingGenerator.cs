using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MeldingGenerator
    {
        private const string FagsystemDefault = "Fagsystem integrasjonstester";

        public static JournalpostHent CreateJournalpostHent(EksternNoekkel referanseEksternNoekkel)
        {
             return new JournalpostHent()
             {
                 ReferanseEksternNoekkel = new EksternNoekkel()
                 {
                     Fagsystem = referanseEksternNoekkel.Fagsystem,
                     Noekkel = referanseEksternNoekkel.Noekkel
                 }
             };
        }
        
        public static MappeHent CreateMappeHent(EksternNoekkel referanseEksternNoekkel)
        {
            return new MappeHent()
            {
                ReferanseEksternNoekkel = new EksternNoekkel()
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

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(string referanseEksternNoekkelNoekkel = null!)
         {
             var arkivmelding = new Arkivmelding()
             {
                 System = FagsystemDefault,
                 MeldingId = Guid.NewGuid().ToString(),
                 AntallFiler = 1,
                 Registrering =
                 {
                     JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault),
                 }
             };
             
             return arkivmelding;
         }

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel;
            
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
                Registrering =
                {
                    journalpost
                }
            };
             
            return arkivmelding;
        }
        
        public static Arkivmelding CreateArkivmelding()
        {
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
            };
             
            return arkivmelding;
        }

        public static Arkivmelding CreateArkivmeldingMedNyttHovedDokument(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel;
            journalpost.Dokumentbeskrivelse.Add(JournalpostGenerator.CreateDokumentbeskrivelse());

            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
                Registrering =
                {
                    journalpost
                }
            };

            return arkivmelding;
        }

        public static Arkivmelding CreateArkivmeldingP??EksisterendeJournalpostMedNyttVedlegg(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel;
            
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
                Registrering =
                {
                    journalpost
                }
            };
             
            return arkivmelding;
        }
        
        public static Arkivmelding CreateArkivmeldingMedSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel, Journalpost journalpost = null)
        {
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                MeldingId = Guid.NewGuid().ToString(),
                AntallFiler = 1,
                Mappe =
                {
                    MappeGenerator.CreateSaksmappe(referanseEksternNoekkelNoekkel, journalpost)
                }
            };
            
            return arkivmelding;
        }

        public static ArkivmeldingOppdatering CreateArkivmeldingOppdateringRegistreringOppdateringNyTittel(EksternNoekkel referanseEksternNoekkel, string nyTittel)
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
        
        public static ArkivmeldingOppdatering CreateArkivmeldingOppdateringSaksmappeOppdateringNySaksansvarlig(EksternNoekkel referanseEksternNoekkel, string nySaksansvarlig)
        {
            return new ArkivmeldingOppdatering()
            {
                MeldingId = Guid.NewGuid().ToString(),
                Tidspunkt = DateTime.Now,
                MappeOppdateringer =
                {
                    new SaksmappeOppdatering()
                    {
                        Saksansvarlig = nySaksansvarlig,
                        ReferanseEksternNoekkel = referanseEksternNoekkel
                    }
                }
            };
        }
    }
}