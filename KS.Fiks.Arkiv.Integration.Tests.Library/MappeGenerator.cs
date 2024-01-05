using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeGenerator
    {
        private const string SaksansvarligDefault = "Sara Saksansvarlig";
        private const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";
            
        public static Mappe CreateSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var saksmappe = new Saksmappe()
            {
                Tittel = SaksmappeTittelDefault,
                Saksansvarlig = new Saksansvarlig()
                {
                    Navn = SaksansvarligDefault
                },
                ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel
            };
            
            return saksmappe;
        }
    }
}