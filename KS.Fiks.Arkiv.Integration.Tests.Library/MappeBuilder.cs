using System.Runtime.CompilerServices;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeBuilder
    {
        private const string SaksansvarligDefault = "Sara Saksansvarlig";
        private const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";

        private Klassifikasjon SaksmappeKlassifikasjon;

        public static MappeBuilder Init()
        {
            return new MappeBuilder();
        }
        
        public Mappe BuildSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel)
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

            if (SaksmappeKlassifikasjon != null)
            {
                saksmappe.Klassifikasjon.Add(SaksmappeKlassifikasjon);
            }
            
            return saksmappe;
        }

        public MappeBuilder WithKlassifikasjon(Klassifikasjon klassifikasjon)
        {
            SaksmappeKlassifikasjon = klassifikasjon;
            return this;
        }
    }
}