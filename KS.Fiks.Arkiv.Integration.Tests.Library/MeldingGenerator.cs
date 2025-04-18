using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MeldingGenerator
    {
        public static MappeHent CreateMappeHent(EksternNoekkel referanseEksternNoekkel, string fagsystem)
        {
            return new MappeHent()
            {
                System = fagsystem,
                ReferanseTilMappe = new ReferanseTilMappe()
                {
                    ReferanseEksternNoekkel = new EksternNoekkel() {
                        Fagsystem = referanseEksternNoekkel.Fagsystem,
                        Noekkel = referanseEksternNoekkel.Noekkel
                    }
                }
            };
        }

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(
            string fagsystem,
            string arkivdel,
            string classSystemId,
            string classId,
            string? tittel = null,
            string? saksbehandlerNavn = null
            )
         {
             var arkivmelding = new Arkivmelding()
             {
                 System = fagsystem,
                 AntallFiler = 1,
                 Registrering = JournalpostBuilder.Init()
                     .WithArkivdel(arkivdel, classSystemId, classId)
                     .WithTittel(tittel)
                     .Build(
                         fagsystem: fagsystem,
                         saksbehandlerNavn: saksbehandlerNavn
                         )
             };
             
             return arkivmelding;
         }
        
        public static Arkivmelding CreateArkivmelding(string fagsystem)
        {
            var arkivmelding = new Arkivmelding()
            {
                System = fagsystem,
                AntallFiler = 1,
            };
             
            return arkivmelding;
        }

        public static Arkivmelding CreateArkivmeldingMedSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel, string fagsystem)
        {
            var arkivmelding = new Arkivmelding()
            {
                System = fagsystem,
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

        public static Arkivmelding CreateArkivmeldingMedNyJournalpost(EksternNoekkel referanseEksternNoekkelNoekkel,
            string arkivdel,
            string classSystemId,
            string classId)
        {
            var journalpost = JournalpostBuilder
                .Init()
                .WithTittel("Test tittel")
                .WithArkivdel(arkivdel, classSystemId, classId)
                .WithEksternNoekkel(referanseEksternNoekkelNoekkel)
                .Build();
            
            var arkivmelding = new Arkivmelding()
            {
                System = referanseEksternNoekkelNoekkel.Fagsystem,
                AntallFiler = 1,
                Registrering = journalpost
            };
             
            return arkivmelding;
        }
    }
}