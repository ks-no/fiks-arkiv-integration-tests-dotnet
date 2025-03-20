using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Exceptions;
using KS.Fiks.Arkiv.Integration.Tests.FiksIO;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Integration.Tests.Library;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Klassifikasjon = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Klassifikasjon;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests
{
    public class IntegrationTestsBase
    {
        protected static List<MottattMeldingArgs>? MottatMeldingArgsList;
        protected static IConfigurationRoot config;
        protected IFiksIOClient? Client;
        protected FiksRequestMessageService? FiksRequestService;
        protected Guid MottakerKontoId;
        protected static string FagsystemNavn;
        protected static string SaksbehandlerNavn;
        protected static string ArkivdelID;
        protected static string KlassifikasjonKlasseID;
        protected static string KlassifikasjonssystemID;
        protected SimpleXsdValidator validator = new SimpleXsdValidator();
        // Tidsavbrudd message is received when a message sent from these tests has not been consumed and the TTL has been reached
        protected const string TidsavbruddMelding = "no.ks.fiks.kvittering.tidsavbrudd";

        protected async Task Init()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste.
            MottatMeldingArgsList = new List<MottattMeldingArgs>();
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Local.json")
                .Build();
            Client = await FiksIOClient.CreateAsync(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            Client.NewSubscription(OnMottattMelding);
            FiksRequestService = new FiksRequestMessageService(config);
            MottakerKontoId = Guid.Parse(GetTestConfig("ArkivAccountId"));
            FagsystemNavn = GetTestConfig("FagsystemName");
            SaksbehandlerNavn = GetTestConfig("SaksbehandlerName");
            ArkivdelID = GetRequiredTestConfig("Arkivdel");
            KlassifikasjonKlasseID = GetRequiredTestConfig("KlassifikasjonKlasseID");
            KlassifikasjonssystemID = GetRequiredTestConfig("KlassifikasjonssystemID");
        }
        
        private string GetTestConfig(string key) => config[$"TestConfig:{key}"] ;

        private string GetRequiredTestConfig(string key)
        {
            var value = GetTestConfig(key);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"The config-key '{key}' is required. See you config-file ({string.Join(", ",config.Providers.OfType<FileConfigurationProvider>().Select(p => p.Source.Path) )})");
            }
            return value;
        }
        
        protected static void VentPaSvar(int antallForventet, int antallVenter)
        {
            var counter = 0;
            while (MottatMeldingArgsList != null && MottatMeldingArgsList.Count <= antallForventet && counter < antallVenter)
            {
                Thread.Sleep(500);
                counter++;
            }
        }

        protected static async void OnMottattMelding(object sender, MottattMeldingArgs mottattMeldingArgs)
        {
            var shortMsgType = MeldingHelper.ShortenMessageType(mottattMeldingArgs);
            // Log Tidsavbrudd messages and discard them
            if (mottattMeldingArgs.Melding.MeldingType == TidsavbruddMelding)
            {
                await Console.Out.WriteLineAsync($"Mottatt {TidsavbruddMelding}. SvarPaMeldingId: {mottattMeldingArgs.Melding.SvarPaMelding}. Ignorerer denne meldingen.");
            }
            await Console.Out.WriteLineAsync($"📨 Mottatt {shortMsgType}-melding med MeldingId: {mottattMeldingArgs.Melding.MeldingId}, SvarPaMeldingId: {mottattMeldingArgs.Melding.SvarPaMelding}, MeldingType: {mottattMeldingArgs.Melding.MeldingType} og lagrer i listen");
            MottatMeldingArgsList?.Add(mottattMeldingArgs);
            mottattMeldingArgs.SvarSender?.Ack();
        }

        protected static void SjekkForventetMelding(IEnumerable<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingsid, string forventetMeldingstype)
        {
            var found = mottattMeldingArgsList.Where(mottattMeldingArgs => mottattMeldingArgs.Melding.SvarPaMelding == sendtMeldingsid).Any(mottatMeldingArgs => mottatMeldingArgs.Melding.MeldingType == forventetMeldingstype);
            var feilmeldinger =
                mottattMeldingArgsList.Where(m => FiksArkivMeldingtype.IsFeilmelding(m.Melding.MeldingType)).ToList();
            if (!found)
            {
                foreach (var mottatMeldingArgs in mottattMeldingArgsList)
                {
                    if (FiksArkivMeldingtype.IsFeilmelding(mottatMeldingArgs.Melding.MeldingType))
                    {
                        var feilmelding = HentUtFeilMelding(mottatMeldingArgs);
                        throw new UnexpectedAnswerException($"Uforventet feilmelding mottatt {mottatMeldingArgs.Melding.MeldingType}. Feilmelding: {feilmelding.Feilmelding}. Forventet meldingstypen {forventetMeldingstype}");
                    }

                    var feilmeldingerString = string.Join(Environment.NewLine,
                        feilmeldinger.Select(m => HentUtFeilMelding(m).Feilmelding));
                    throw new UnexpectedAnswerException($"Uforventet melding mottatt av typen {mottatMeldingArgs.Melding.MeldingType}. Forventet meldingstypen {forventetMeldingstype}.{Environment.NewLine} Feilmeldinger: {feilmeldinger.Count}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine} {feilmeldingerString}");  
                }
            }
            Console.Out.WriteLineAsync($"🎉 Forventet meldingstype {forventetMeldingstype} mottatt for meldingsid  {sendtMeldingsid}!");
        }

        protected static void SjekkForventetMeldinger(IEnumerable<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingsid, string[] forventetMeldingstyper)
        {
            foreach (var forventetMeldingstype in forventetMeldingstyper)
            {
                var found = mottattMeldingArgsList.Where(mottattMeldingArgs => mottattMeldingArgs.Melding.SvarPaMelding == sendtMeldingsid).Any(mottatMeldingArgs => mottatMeldingArgs.Melding.MeldingType == forventetMeldingstype);
                var feilmeldinger =
                    mottattMeldingArgsList.Where(m => FiksArkivMeldingtype.IsFeilmelding(m.Melding.MeldingType)).ToList();
                
                if (!found)
                {
                    foreach (var mottatMeldingArgs in mottattMeldingArgsList)
                    {
                        if (forventetMeldingstyper.Contains(mottatMeldingArgs.Melding.MeldingType))
                        {
                            continue;
                        }
                        if (FiksArkivMeldingtype.IsFeilmelding(mottatMeldingArgs.Melding.MeldingType))
                        {
                            var feilmelding = HentUtFeilMelding(mottatMeldingArgs);
                            throw new UnexpectedAnswerException($"Uforventet feilmelding mottatt {mottatMeldingArgs.Melding.MeldingType}. Feilmelding: {feilmelding.Feilmelding}. Forventet meldingstypen {forventetMeldingstype}");
                        }

                        var feilmeldingerString = string.Join(Environment.NewLine,
                            feilmeldinger.Select(m => HentUtFeilMelding(m).Feilmelding));
                        throw new UnexpectedAnswerException($"Uforventet melding mottatt av typen {mottatMeldingArgs.Melding.MeldingType}. Forventet meldingstypen {forventetMeldingstype}.{Environment.NewLine} Feilmeldinger: {feilmeldinger.Count}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine} {feilmeldingerString}");  
                    }
                }
                Console.Out.WriteLineAsync($"🎉 Forventet meldingstype {forventetMeldingstype} mottatt for meldingsid  {sendtMeldingsid}!");
            }
        }

        protected static FeilmeldingBase HentUtFeilMelding(MottattMeldingArgs mottatMeldingArgs)
        {
            var melding = MeldingHelper.GetDecryptedMessagePayloadAsync(mottatMeldingArgs).Result;
            var xml = melding.PayloadAsString;
            FeilmeldingBase feilmelding;
            switch (mottatMeldingArgs.Melding.MeldingType)
            {
                case FiksArkivMeldingtype.Ugyldigforespørsel:
                    feilmelding = SerializeHelper.DeserializeXml<Ugyldigforespoersel>(xml);
                    break;
                case FiksArkivMeldingtype.Serverfeil:
                    feilmelding = SerializeHelper.DeserializeXml<Serverfeil>(xml);
                    break;
                case FiksArkivMeldingtype.Ikkefunnet:
                    feilmelding = SerializeHelper.DeserializeXml<Ikkefunnet>(xml);
                    break;
                default:
                    throw new NotImplementedException($"Ikke implementert for {mottatMeldingArgs.Melding.MeldingType}");
            }

            return feilmelding;
        }
        

        protected static MottattMeldingArgs? GetMottattMelding(List<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingsid, string forventetMeldingstype)
        {
            foreach (var mottatMeldingArgs in mottattMeldingArgsList)
            {
                if (mottatMeldingArgs.Melding.SvarPaMelding == sendtMeldingsid)
                {
                    if (mottatMeldingArgs.Melding.MeldingType == forventetMeldingstype)
                    {
                        mottattMeldingArgsList.Remove(mottatMeldingArgs);
                        return mottatMeldingArgs;
                    }
                }        
            }

            return null;
        }
        
        protected static bool HarFeilmelding(List<MottattMeldingArgs> mottattMeldingArgsList, Guid sendtMeldingid)
        {
            foreach (var mottatMeldingArgs in mottattMeldingArgsList)
            {
                if (mottatMeldingArgs.Melding.SvarPaMelding == sendtMeldingid)
                {
                    if (FiksArkivMeldingtype.IsFeilmelding(mottatMeldingArgs.Melding.MeldingType))
                    {
                        Console.Out.WriteLineAsync($"Svar med feilmelding på vår melding med meldingId {sendtMeldingid} mottatt. Melding er av typen: {mottatMeldingArgs.Melding.MeldingType}");

                        return true;
                    }
                }        
            }
            return false;
        }
        
        protected static bool Ikkfunnet(List<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingid)
        {
            foreach (var mottatMeldingArgs in mottattMeldingArgsList)
            {
                if (mottatMeldingArgs.Melding.SvarPaMelding == sendtMeldingid)
                {
                    if (FiksArkivMeldingtype.Ikkefunnet == mottatMeldingArgs.Melding.MeldingType)
                    {
                        Console.Out.WriteLineAsync($"Svar med feilmelding på vår melding med meldingId {sendtMeldingid} mottatt. Melding er som forventet av typen: {mottatMeldingArgs.Melding.MeldingType}");
                        return true;
                    }
                }        
            }
            return false;
        }

        protected async Task<Mappe?> HentMappe(string testSessionId, EksternNoekkel referanseEksternNoekkel)
        {
            var mappeHent = MeldingGenerator.CreateMappeHent(referanseEksternNoekkel, FagsystemNavn);
            
            var mappeHentSerialized = SerializeHelper.Serialize(mappeHent);
            
            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("MappeHent.xml", mappeHentSerialized);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentSerialized);
            
            // Nullstill meldingsliste
            MottatMeldingArgsList.Clear();
            
            // Send hent melding
            var mappeHentMeldingId = await FiksRequestService.SendAsync(MottakerKontoId, FiksArkivMeldingtype.MappeHent, mappeHentSerialized, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            if (Ikkfunnet(MottatMeldingArgsList, mappeHentMeldingId))
            {
                return null; 
            }
            
            // Verifiser at man får mappeHentResultat melding
            var mappeHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.That(mappeHentResultatMelding != null);
            
            var mappeHentResultatPayload = await MeldingHelper.GetDecryptedMessagePayloadAsync(mappeHentResultatMelding);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = SerializeHelper.DeserializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

            return mappeHentResultat.Mappe;
        }
        
        protected async Task<ArkivmeldingKvittering> OpprettSaksmappe(string testSessionId, EksternNoekkel referanseEksternNoekkel)
        {
            Console.Out.WriteLine($"Sender arkivmelding med ny saksmappe med EksternNoekkel fagsystem {referanseEksternNoekkel.Fagsystem} og noekkel {referanseEksternNoekkel.Noekkel}");

            var arkivmeldingNySaksmappe = MeldingGenerator.CreateArkivmeldingMedSaksmappe(referanseEksternNoekkel, FagsystemNavn);

            var nySaksmappeSerialized = SerializeHelper.Serialize(arkivmeldingNySaksmappe);

            // Valider arkivmelding
            validator.Validate(nySaksmappeSerialized);

            // Utkommenter dette hvis man vil å skrive til fil for å sjekke resultat manuelt
            //File.WriteAllText("ArkivmeldingMedNySaksmappe2.xml", nySaksmappeSerialized);

            // Send melding
            var nySaksmappeMeldingId = await FiksRequestService.SendAsync(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett,
                nySaksmappeSerialized, null, testSessionId);

            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.That(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId,
                FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);

            var arkivmeldingKvitteringPayload = await MeldingHelper.GetDecryptedMessagePayloadAsync(arkivmeldingKvitteringMelding);
            Assert.That(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml",
                "Filnavn ikke som forventet arkivmelding-kvittering.xml");

            // Valider innhold i kvitteringen (xml)
            validator.Validate(arkivmeldingKvitteringPayload.PayloadAsString);

            var arkivmeldingKvittering =
                SerializeHelper.DeserializeXml<ArkivmeldingKvittering>(arkivmeldingKvitteringPayload.PayloadAsString);
            return arkivmeldingKvittering;
        }
        
        protected static Klassifikasjon GenererKlassifikasjon() => new ()
            {
                KlasseID = KlassifikasjonKlasseID,
                KlassifikasjonssystemID = KlassifikasjonssystemID
            };
            protected static EksternNoekkel GenererEksternNoekkel( string? noekkel = null) => new ()
            {
                Fagsystem = FagsystemNavn,
                Noekkel = noekkel ?? Guid.NewGuid().ToString()
            };
    }
}
