using System;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Metadatakatalog.EksternNoekkel;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class SokBuilder
    {
        private Sokdefinisjon? _sokdefinisjon;
        
        public static SokBuilder Init()
        {
            return new SokBuilder();
        }

        public SokBuilder WithMatrikkelSokdefinisjon(Sokdefinisjon sokdefinisjon)
        {
            _sokdefinisjon = sokdefinisjon;
            return this;
        }

        public SokBuilder WithSaksmappeMatrikkelSok(int gaardsnummer, int bruksnummer, int seksjonsnummer, int festenummer, int kommunenummer)
        {
            _sokdefinisjon = new SaksmappeSokdefinisjon()
            {
                Parametere = { 
                    new SaksmappeParameter()
                    {
                        Felt = SaksmappeSokefelt.SakMatrikkelnummerBruksnummer,
                        Operator = OperatorType.Equal,
                        SokVerdier = new SokVerdier()
                        {
                            Intvalues = { bruksnummer }
                        }
                    },
                    new SaksmappeParameter()
                    {
                        Felt = SaksmappeSokefelt.SakMatrikkelnummerGaardsnummer,
                        Operator = OperatorType.Equal,
                        SokVerdier = new SokVerdier()
                        {
                            Intvalues = { gaardsnummer }
                        }
                    },
                    new SaksmappeParameter()
                    {
                        Felt = SaksmappeSokefelt.SakMatrikkelnummerFestenummer,
                        Operator = OperatorType.Equal,
                        SokVerdier = new SokVerdier()
                        {
                            Intvalues = { festenummer }
                        }
                    },
                    new SaksmappeParameter()
                    {
                        Felt = SaksmappeSokefelt.SakMatrikkelnummerKommunenummer,
                        Operator = OperatorType.Equal,
                        SokVerdier = new SokVerdier()
                        {
                            Intvalues = { kommunenummer }
                        }
                    },
                    new SaksmappeParameter()
                    {
                        Felt = SaksmappeSokefelt.SakMatrikkelnummerSeksjonsnummer,
                        Operator = OperatorType.Equal,
                        SokVerdier = new SokVerdier()
                        {
                            Intvalues = { seksjonsnummer }
                        }
                    },
                }
            };
            return this;
        }

        public SokBuilder WithResponstype(Responstype responstype)
        {
            if (_sokdefinisjon == null)
            {
                throw new ArgumentException("Type søk er ikke definert og kan dermed ikke sette responstype");
            }
            _sokdefinisjon.Responstype = responstype;
            return this;
        }

        public Sok Build()
        {
            return new Sok()
            {
                System = "System",
                Sokdefinisjon = _sokdefinisjon,
            };
        }
    }
}