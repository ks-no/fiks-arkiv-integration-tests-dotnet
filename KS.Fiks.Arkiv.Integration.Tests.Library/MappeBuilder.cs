using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeBuilder
    {
        private string _tittel = "En ny saksmappe fra integrasjonstest";
        private AdministrativEnhet _administrativEnhet;
        private Saksansvarlig _saksansvarlig;
        private static List<Klassifikasjon> SaksmappeKlassifikasjoner;

        public static MappeBuilder Init()
        {
            SaksmappeKlassifikasjoner = new List<Klassifikasjon>();
            return new MappeBuilder();
        }
        
        public Mappe BuildSaksmappe(EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var saksmappe = new Saksmappe()
            {
                Tittel = _tittel,
                Saksansvarlig = _saksansvarlig,
                AdministrativEnhet = _administrativEnhet,
                ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel
            };

            foreach (var klassifikasjon in SaksmappeKlassifikasjoner)
            {
                saksmappe.Klassifikasjon.Add(klassifikasjon);
            }

            return saksmappe;
        }

        public MappeBuilder WithKlassifikasjon(Klassifikasjon klassifikasjon)
        {
            SaksmappeKlassifikasjoner.Add(klassifikasjon);
            return this;
        }

        public MappeBuilder WithTittel(string tittel)
        {
            _tittel = tittel;
            return this;
        }

        public MappeBuilder WithAdministrativEnhet(AdministrativEnhet administrativEnhet)
        {
            _administrativEnhet = administrativEnhet;
            return this;
        }

        public MappeBuilder WithSaksansvarlig(Saksansvarlig saksansvarlig)
        {
            _saksansvarlig = saksansvarlig;
            return this;
        }
    }
}