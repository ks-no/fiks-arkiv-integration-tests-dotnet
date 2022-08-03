using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Dokumentbeskrivelse = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentbeskrivelse;
using Dokumentobjekt = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentobjekt;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.EksternNoekkel;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost;
using Korrespondansepart = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Korrespondansepart;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class JournalpostGenerator
    {
        public const string SaksansvarligDefault = "Sara Saksansvarlig";
        public const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";
        public const string FagsystemDefault = "Fagsystem validatortester";
        public const string ArkivdelDefault = "Arkiv validatortester";
            

        public static Journalpost CreateJournalpost(ReferanseForelderMappe referanseForelderMappe)
        {
            var jp = CreateJournalpost();
            jp.ReferanseForelderMappe = referanseForelderMappe;
            return jp;
        }
             
        public static Journalpost CreateJournalpost(string referanseArkivdel)
        {
            var jp = CreateJournalpost();
            jp.Arkivdel = new Kode() {KodeProperty = referanseArkivdel};
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
                            Versjonsnummer = "1",
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
        
        private static Journalpost CreateJournalpostMedDokumenter()
        {
            var jp = CreateJournalpost();
            jp.Dokumentbeskrivelse.Add(
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
                            Versjonsnummer = "1",
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
                });
            return jp;
        } 
        
        private static Journalpost CreateJournalpost()
        {
            return new Journalpost()
            {
                OpprettetAv = "En brukerid",
                ArkivertAv = "En brukerid",
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = FagsystemDefault,
                    Noekkel = Guid.NewGuid().ToString()
                },
                Tittel = "Internt notat",
                Korrespondansepart =
                {
                    new Korrespondansepart()
                    {
                        Korrespondanseparttype = new Korrespondanseparttype()
                        {
                            KodeProperty= "IM"
                        },
                        KorrespondansepartNavn = "Oppmålingsetaten",
                        AdministrativEnhet = "Oppmålingsetaten",
                        Saksbehandler = "Ingrid Mottaker"
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