using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MeldingGenerator
    {
        private const string FagsystemDefault = "Fagsystem integrasjonstester";

        public static RegistreringHent CreateJournalpostHent(EksternNoekkel referanseEksternNoekkel)
        {
             return new RegistreringHent()
             {
                 ReferanseTilRegistrering = new ReferanseTilRegistrering()
                 {
                     ReferanseEksternNoekkel = new EksternNoekkel() {
                        Fagsystem = referanseEksternNoekkel.Fagsystem,
                        Noekkel = referanseEksternNoekkel.Noekkel
                     }
                 }
             };
        }
        
        public static MappeHent CreateMappeHent(EksternNoekkel referanseEksternNoekkel)
        {
            return new MappeHent()
            {
                ReferanseTilMappe = new ReferanseTilMappe()
                {
                    ReferanseEksternNoekkel = new EksternNoekkel() {
                        Fagsystem = referanseEksternNoekkel.Fagsystem,
                        Noekkel = referanseEksternNoekkel.Noekkel
                    }
                }
            };
        }
        
        public static RegistreringHent CreateJournalpostHent(SystemID systemId )
        {
            return new RegistreringHent()
            {
                ReferanseTilRegistrering = new ReferanseTilRegistrering(){
                  SystemID = new SystemID()
                   {
                       Label = systemId.Label,
                       Value = systemId.Value
                   }
               },
            };
        }

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(string referanseEksternNoekkelNoekkel = null!, string tittel = null)
         {
             var arkivmelding = new Arkivmelding()
             {
                 System = FagsystemDefault,
                 AntallFiler = 1,
                 Registrering = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault, tittel),
             };
             
             return arkivmelding;
         }
        
        public static Arkivmelding CreateArkivmelding()
        {
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
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
                AntallFiler = 1,
                Registrering = journalpost
            };

            return arkivmelding;
        }

        public static Arkivmelding CreateArkivmeldingPåEksisterendeJournalpostMedNyttVedlegg(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault);
            journalpost.ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel;
            
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                AntallFiler = 1,
                Registrering = journalpost
            };
             
            return arkivmelding;
        }
        
        public static Arkivmelding CreateArkivmeldingMedSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                AntallFiler = 1,
                Mappe = MappeBuilder.Init().BuildSaksmappe(referanseEksternNoekkelNoekkel)
            };
            
            return arkivmelding;
        }

        public static ArkivmeldingOppdatering CreateArkivmeldingOppdateringRegistreringOppdateringNyTittel(ReferanseTilRegistrering referanseTilRegistrering, string nyTittel)
        {
            return new ArkivmeldingOppdatering()
            {
                RegistreringOppdateringer =
                {
                    new RegistreringOppdatering()
                    {
                        Tittel = nyTittel,
                        ReferanseTilRegistrering = referanseTilRegistrering
                    }
                }
            };
        }
        
        public static ArkivmeldingOppdatering CreateArkivmeldingOppdateringSaksmappeOppdateringNySaksansvarlig(EksternNoekkel referanseEksternNoekkel, string nySaksansvarlig)
        {
            return new ArkivmeldingOppdatering()
            {
                MappeOppdateringer =
                {
                    new SaksmappeOppdatering()
                    {
                        ReferanseTilMappe = new ReferanseTilMappe()
                        {
                            ReferanseEksternNoekkel = referanseEksternNoekkel
                        },
                        Saksansvarlig = new Saksansvarlig()
                        {
                            Navn = nySaksansvarlig,
                        }
                    }
                }
            };
        }

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var journalpost = JournalpostGenerator.CreateJournalpost(JournalpostGenerator.ArkivdelDefault);
            journalpost.ReferanseEksternNoekkel = new EksternNoekkel()
            {
                Fagsystem = referanseEksternNoekkelNoekkel.Fagsystem,
                Noekkel = referanseEksternNoekkelNoekkel.Noekkel
            };
            
            var arkivmelding = new Arkivmelding()
            {
                System = FagsystemDefault,
                AntallFiler = 1,
                Registrering = journalpost
            };
             
            return arkivmelding;
        }
    }
}