using System;
using System.Collections.Generic;
using System.Linq;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class JournalpostBuilder
    {
        public const string SaksansvarligDefault = "Sara Saksansvarlig";
        public const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";
        public const string FagsystemDefault = "Fagsystem validatortester";
        public const string ArkivdelDefault = "Arkiv validatortester";

        private ReferanseTilMappe _referanseTilMappe;
        private EksternNoekkel _eksternNoekkel;
        private string _tittel;
        private string _arkivdel;
        private List<Dokumentbeskrivelse> _dokumentbeskrivelser = new List<Dokumentbeskrivelse>();
        private string _journalposttype;
        private string _journalposttypeBeskrivelse;
        private string _journalstatus;
        private string _offentligTittel;
        private List<Korrespondansepart> _korrespondanseparter = new List<Korrespondansepart>();

        public static JournalpostBuilder Init()
        {
            return new JournalpostBuilder();
        }

        public JournalpostBuilder WithReferanseTilForelderMappe(ReferanseTilMappe referanseTilMappe)
        {
            _referanseTilMappe = referanseTilMappe;
            return this;
        }
        
        public JournalpostBuilder WithTittel(string tittel)
        {
            _tittel = tittel;
            return this;
        }
        
        public JournalpostBuilder WithArkivdel(string arkivdel)
        {
            _arkivdel = arkivdel;
            return this;
        }
        
        public JournalpostBuilder WithEksternNoekkel(EksternNoekkel eksternNoekkel)
        {
            _eksternNoekkel = eksternNoekkel;
            return this;
        }

        public JournalpostBuilder WithDokumentbeskrivelse(Dokumentbeskrivelse dokumentbeskrivelse)
        {
            _dokumentbeskrivelser.Add(dokumentbeskrivelse);
            return this;
        }

        public Journalpost Build(
            string? saksbehandlerNavn = null,
            string? fagsystem = null
            )
        {
            var jp = CreateJournalpost(
                saksbehandlerNavn: saksbehandlerNavn,
                fagsystem: fagsystem
                );

            if (_dokumentbeskrivelser.Count > 0)
            {
                foreach (var dokumentbeskrivelse in _dokumentbeskrivelser)
                {
                    jp.Dokumentbeskrivelse.Add(dokumentbeskrivelse);
                }
            }

            if (_korrespondanseparter.Count > 0)
            {
                foreach (var korrespondansepart in _korrespondanseparter)
                {
                    jp.Korrespondansepart.Add(korrespondansepart);
                }
            }

            if (_arkivdel != null)
            {
                jp.Arkivdel = new Kode() { KodeProperty = _arkivdel };
            }

            jp.ReferanseForelderMappe = _referanseTilMappe;
            jp.Tittel = _tittel;
            jp.OffentligTittel = _offentligTittel;
            jp.Journalposttype = new Journalposttype()
            {
                KodeProperty = _journalposttype
            };

            jp.Journalstatus = new Journalstatus()
            {
                KodeProperty = _journalstatus,
                Beskrivelse = _journalposttypeBeskrivelse
            };
            jp.ReferanseEksternNoekkel = _eksternNoekkel;

            return jp;
        }

        private Journalpost CreateJournalpost(
            string? saksbehandlerNavn = null,
            string? fagsystem = null
            )
        {
            return new Journalpost()
            {
                OpprettetAv = "En brukerid",
                ArkivertAv = "En brukerid",
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = fagsystem ?? FagsystemDefault,
                    Noekkel = Guid.NewGuid().ToString()
                },
                Tittel = "Internt notat",
                Korrespondansepart =
                {
                    new Korrespondansepart()
                    {
                        Korrespondanseparttype = new Korrespondanseparttype()
                        {
                            KodeProperty = "IM"
                        },
                        KorrespondansepartNavn = "Oppmålingsetaten",
                        AdministrativEnhet = new AdministrativEnhet()
                        {
                            Navn = "Oppmålingsetaten",
                        },
                        Saksbehandler = new Saksbehandler() { 
                            
                            Navn = saksbehandlerNavn ?? "Ingrid Mottaker"
                        }
                    }
                },
                Journalposttype = new Journalposttype()
                {
                    KodeProperty= "X"
                },
                Journalstatus = new Journalstatus()
                {
                    KodeProperty= "F"
                },
                DokumentetsDato = DateTime.Now.Date,
                MottattDato = DateTime.Now,
            };
        }

        public JournalpostBuilder WithJournalposttype(string journalposttype, string beskrivelse = "")
        {
            _journalposttype = journalposttype;
            _journalposttypeBeskrivelse = beskrivelse;
            return this;
        }

        public JournalpostBuilder WithJournalstatus(string journalstatus)
        {
            _journalstatus = journalstatus;
            return this;
        }

        public JournalpostBuilder WithOffentligTittel(string offentligTittel)
        {
            _offentligTittel = offentligTittel;
            return this;
        }

        public JournalpostBuilder WithKorrespondansepart(Korrespondansepart korrespondansepart)
        {
            _korrespondanseparter.Add(korrespondansepart);
            return this;
        }
    }
}
