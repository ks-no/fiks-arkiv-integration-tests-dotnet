using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeGenerator
    {
        public const string SaksansvarligDefault = "Sara Saksansvarlig";
        public const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";
            
        public static Mappe CreateSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel, Journalpost journalpost)
        {
            var saksmappe = new Saksmappe()
            {
                Tittel = SaksmappeTittelDefault,
                Saksansvarlig = SaksansvarligDefault,
                ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel
            };
            if (journalpost != null)
            {
                saksmappe.Registrering.Add(journalpost);
            }
            return saksmappe;
        }

        public static Mappe CreateSaksmappe(Models.V1.Arkivstruktur.EksternNoekkel referanseEksternNoekkelNoekkel, Journalpost journalpost)
        {
            var saksmappe = new Saksmappe()
            {
                Tittel = SaksmappeTittelDefault,
                Saksansvarlig = SaksansvarligDefault,
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = referanseEksternNoekkelNoekkel.Fagsystem,
                    Noekkel = referanseEksternNoekkelNoekkel.Noekkel
                }
            };
            if (journalpost != null)
            {
                saksmappe.Registrering.Add(journalpost);
            }
            return saksmappe;
        }
    }
}