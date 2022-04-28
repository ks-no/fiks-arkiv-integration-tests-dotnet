using System;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Dokumentbeskrivelse = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentbeskrivelse;
using Dokumentobjekt = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentobjekt;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost;
using Korrespondansepart = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Korrespondansepart;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Saksmappe;

namespace KS.Fiks.Arkiv.Integration.Tests.Library
{
    public class ArkivmeldingDataGenerator
    {
        private const string SaksansvarligDefault = "Sara Saksansvarlig";
        private const string SaksmappeTittelDefault = "En ny saksmappe fra integrasjonstest";

        public static Journalpost CreateJournalpost()
        {
            return new Journalpost()
            {
                OpprettetAv = "En brukerid",
                ArkivertAv = "En brukerid",
                ReferanseForelderMappe = new SystemID() { Label = "", Value = Guid.NewGuid().ToString() },
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = "Fagsystem X",
                    Noekkel = Guid.NewGuid().ToString()
                },
                Dokumentbeskrivelse =
                {
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
                    }
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
        
        public static Saksmappe CreateSaksmappe(Journalpost? journalpost, EksternNoekkel referanseEksternNoekkelNoekkel)
        {
            var saksmappe = new Saksmappe()
            {
                Tittel = SaksmappeTittelDefault,
                Saksansvarlig = SaksansvarligDefault,
                ReferanseEksternNoekkel = referanseEksternNoekkelNoekkel
            };
            if (journalpost != null)
            {
                saksmappe.Registrering.Add(journalpost);
            }
            return saksmappe;
        }
    }
}