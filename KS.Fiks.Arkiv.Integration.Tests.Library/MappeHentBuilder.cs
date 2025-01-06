using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class MappeHentBuilder
    {
        private EksternNoekkel _eksternNoekkel;
        private SystemID _systemId;
        
        public static MappeHentBuilder Init()
        {
            return new MappeHentBuilder();
        }

        public MappeHentBuilder WithEksternNoekkel(EksternNoekkel eksternNoekkel)
        {
            _eksternNoekkel = eksternNoekkel;
            return this;
        }

        public MappeHentBuilder WithSystemID(SystemID systemId)
        {
            _systemId = systemId;
            return this;
        }

        public MappeHent Build()
        {
            return new MappeHent()
            {
                System = "System",
                ReferanseTilMappe = new ReferanseTilMappe()
                {
                    ReferanseEksternNoekkel = _eksternNoekkel,
                    SystemID = _systemId
                }
            };
        }
    }
}