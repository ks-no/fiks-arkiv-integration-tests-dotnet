using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class RegistreringHentBuilder
    {
        private RegistreringHent _registreringHent;
        private EksternNoekkel _eksternNoekkel;
        private SystemID _systemId;
        
        public static RegistreringHentBuilder Init()
        {
            return new RegistreringHentBuilder();
        }

        public RegistreringHentBuilder WithEksternNoekkel(EksternNoekkel eksternNoekkel)
        {
            _eksternNoekkel = eksternNoekkel;
            return this;
        }

        public RegistreringHentBuilder WithSystemID(SystemID systemId)
        {
            _systemId = systemId;
            return this;
        }

        public RegistreringHent Build()
        {
            return new RegistreringHent()
            {
                System = "System",
                ReferanseTilRegistrering = new ReferanseTilRegistrering
                {
                    ReferanseEksternNoekkel = _eksternNoekkel,
                    SystemID = _systemId
                }
            };
            
        }
    }
}