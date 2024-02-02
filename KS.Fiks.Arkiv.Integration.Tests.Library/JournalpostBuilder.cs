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

        public JournalpostBuilder WithDokumentbeskrivelse()
        {
            _dokumentbeskrivelse = CreateDokumentbeskrivelse();
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
            jp.Tittel = _tittel;
            return jp;
        }
        
        public static Dokumentbeskrivelse CreateDokumentbeskrivelse()
        {
            var dokumentbeskrivelse = 
                new Dokumentbeskrivelse()
                {
                    Dokumenttype = new Dokumenttype()
                    {
                        KodeProperty= "SØKNAD"
                    },
                    Dokumentstatus = new Dokumentstatus()
                    {
                        KodeProperty= "F"
                    },
                    Tittel = "Rekvisisjon av oppmålingsforretning",
                    TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                    {
                        KodeProperty= "H"
                    },
                    Dokumentobjekt =
                    {
                        new Dokumentobjekt()
                        {
                            Versjonsnummer = 1,
                            Variantformat = new Variantformat()
                            {
                                KodeProperty= "P"
                            },
                            Format = new Format()
                            {
                                KodeProperty= "PDF"
                            },
                            Filnavn = "rekvisjon.pdf",
                            ReferanseDokumentfil = "rekvisisjon.pdf"
                        }
                    }
                };
            return dokumentbeskrivelse;
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
    }
}
