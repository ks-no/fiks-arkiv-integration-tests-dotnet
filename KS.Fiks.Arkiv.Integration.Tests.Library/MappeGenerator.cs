using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Saksmappe;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeGenerator
    {
        public const string SaksansvarligDefault = "Sara Saksansvarlig";
        public const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";
            
        public static Saksmappe CreateSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel, Journalpost? journalpost)
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
    }
}