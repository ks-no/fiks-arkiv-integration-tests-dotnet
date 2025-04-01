using System.Collections.Generic;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class DokumentbeskrivelseBuilder
    {
        private string _tittel;
        private string _dokumenttype = "SØKNAD";
        private string? _dokumenttypebeskrivelse;
        private string _dokumentstatus = "F";
        private string? _dokumentstatusbeskrivelse;
        private string _tilknyttetRegistreringSom = "F";
        private string? _tilknyttetRegistreringSomBeskrivelse;
        private static List<Dokumentobjekt>? _dokumentobjekter;

        public static DokumentbeskrivelseBuilder Init()
        {
            _dokumentobjekter = new List<Dokumentobjekt>();
            return new DokumentbeskrivelseBuilder();
        }

        public DokumentbeskrivelseBuilder WithTittel(string tittel)
        {
            _tittel = tittel;
            return this;
        }

        public DokumentbeskrivelseBuilder WithDokumenttype(string dokumenttype, string? beskrivelse = "")
        {
            _dokumenttype = dokumenttype;
            _dokumenttypebeskrivelse = beskrivelse;
            return this;
        }

        public DokumentbeskrivelseBuilder WithDokumentstatus(string dokumentstatus, string? beskrivelse = "")
        {
            _dokumentstatus = dokumentstatus;
            _dokumentstatusbeskrivelse = beskrivelse;
            return this;
        }

        public DokumentbeskrivelseBuilder WithTilknyttetRegistreringSom(string tilknyttetRegistreringSom, string beskrivelse = "")
        {
            _tilknyttetRegistreringSom = tilknyttetRegistreringSom;
            _tilknyttetRegistreringSomBeskrivelse = beskrivelse;
            return this;
        }

        public Dokumentbeskrivelse Build()
        {
            var dokumentbeskrivelse = new Dokumentbeskrivelse()
            {
                Tittel = _tittel,
                Dokumenttype = new Dokumenttype()
                {
                    KodeProperty = _dokumenttype,
                    Beskrivelse = _dokumenttypebeskrivelse
                },
                Dokumentstatus = new Dokumentstatus()
                {
                    KodeProperty = _dokumentstatus,
                    Beskrivelse = _dokumentstatusbeskrivelse
                },
                TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                {
                    KodeProperty = _tilknyttetRegistreringSom,
                    Beskrivelse = _tilknyttetRegistreringSomBeskrivelse
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
