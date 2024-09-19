using System;
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
        private string _tittel;
        private string _arkivdel;
        private Dokumentbeskrivelse _dokumentbeskrivelse;
        private string _journalposttype;
        private string _journalstatus;

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

        public JournalpostBuilder WithDokumentbeskrivelse(Dokumentbeskrivelse dokumentbeskrivelse)
        {
            _dokumentbeskrivelse = dokumentbeskrivelse;
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
            jp.Dokumentbeskrivelse.Add(_dokumentbeskrivelse);
            if (_arkivdel != null)
            {
                jp.Arkivdel = new Kode() {KodeProperty = _arkivdel};
            }
            jp.ReferanseForelderMappe = _referanseTilMappe;
            if (_tittel != null)
            {
                jp.Tittel = _tittel;
            }

            if (_journalposttype != null)
            {
                jp.Journalposttype = new Journalposttype()
                {
                    KodeProperty = _journalposttype
                };
            }

            if (_journalstatus != null)
            {
                jp.Journalstatus = new Journalstatus()
                {
                    KodeProperty = _journalstatus
                };
            }

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

        public JournalpostBuilder WithJournalposttype(string journalposttype)
        {
            _journalposttype = journalposttype;
            return this;
        }

        public JournalpostBuilder WithJournalstatus(string journalstatus)
        {
            _journalstatus = journalstatus;
            return this;
        }
    }
}
