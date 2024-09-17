using System;
using System.Collections.Generic;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class DokumentbeskrivelseBuilder
    {
        private string _tittel;
        private string _dokumenttype = "SØKNAD";
        private string _dokumentstatus = "F";
        private string _tilknyttetRegistreringSom = "F";
        private List<Dokumentobjekt> _dokumentobjekter;

        public static DokumentbeskrivelseBuilder Init()
        {
            return new DokumentbeskrivelseBuilder();
        }

        public DokumentbeskrivelseBuilder WithTittel(string tittel)
        {
            _tittel = tittel;
            return this;
        }

        public DokumentbeskrivelseBuilder WithDokumenttype(string dokumenttype)
        {
            _dokumenttype = dokumenttype;
            return this;
        }

        public DokumentbeskrivelseBuilder WithDokumentstatus(string dokumentstatus)
        {
            _dokumentstatus = dokumentstatus;
            return this;
        }

        public DokumentbeskrivelseBuilder WithTilknyttetRegistreringSom(string tilknyttetRegistreringSom)
        {
            _tilknyttetRegistreringSom = tilknyttetRegistreringSom;
            return this;
        }

        public Dokumentbeskrivelse Build()
        {
            var dokumentbeskrivelse = new Dokumentbeskrivelse()
            {
                Tittel = _tittel,
                Dokumenttype = new Dokumenttype()
                {
                    KodeProperty = _dokumenttype
                },
                Dokumentstatus = new Dokumentstatus()
                {
                    KodeProperty = _dokumentstatus
                },
                TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                {
                    KodeProperty = _tilknyttetRegistreringSom
                }
            };

            foreach (var dokumentobjekt in _dokumentobjekter)
            {
                dokumentbeskrivelse.Dokumentobjekt.Add(dokumentobjekt);
            }

            return dokumentbeskrivelse;
        }

        public DokumentbeskrivelseBuilder WithDokumentobjekt(Dokumentobjekt dokumentobjekt)
        {
            _dokumentobjekter.Add(dokumentobjekt);
            return this;
        }
    }
}
