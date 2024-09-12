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
        public List<MottattMeldingArgs>? MottatMeldingArgsList;
        public string TestSessionId;
        public static IConfigurationRoot config;
        public IFiksIOClient? Client;
        public FiksRequestMessageService? FiksRequestService;
        public Guid MottakerKontoId;
        public static string FagsystemNavn;
        public static string SaksbehandlerNavn;
        public static string KlassifikasjonKlasseID;
        public static string KlassifikasjonssystemID;
        public SimpleXsdValidator validator;
        public Action<object, MottattMeldingArgs>? onMottatMelding;

        protected async Task Init()
        {
            //TODO En annen lokal lagring som kjørte for disse testene hadde vært stilig i stedet for en liste.
            await ConfInit();
            MottatMeldingArgsList = new List<MottattMeldingArgs>();
            Client = await FiksIOClient.CreateAsync(FiksIOConfigurationBuilder.CreateFiksIOConfiguration(config));
            Client.NewSubscription(OnMottattMelding);
        }
        protected async Task ConfInit()
        {
            MottatMeldingArgsList = new List<MottattMeldingArgs>();
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Local.json")
                .Build();
            FiksRequestService = new FiksRequestMessageService(config);
            TestSessionId = Guid.NewGuid().ToString();
            MottakerKontoId = Guid.Parse(GetTestConfig("ArkivAccountId"));
            FagsystemNavn = GetTestConfig("FagsystemName");
            SaksbehandlerNavn = GetTestConfig("SaksbehandlerName");
            KlassifikasjonKlasseID = GetRequiredTestConfig("KlassifikasjonKlasseID") ;
            KlassifikasjonssystemID = GetRequiredTestConfig("KlassifikasjonssystemID") ;
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
        
        public void VentPaSvar(int antallForventet, int antallVenter, List<MottattMeldingArgs>? mottattMeldingArgsEnumerable = null)
        {
            var counter = 0;
            if (mottattMeldingArgsEnumerable == null)
            {
                mottattMeldingArgsEnumerable = MottatMeldingArgsList;

            }
            while (mottattMeldingArgsEnumerable != null && mottattMeldingArgsEnumerable.Count <= antallForventet && counter < antallVenter)
            {
                Thread.Sleep(500);
                counter++;
            }
        }

         async void OnMottattMelding(object sender, MottattMeldingArgs mottattMeldingArgs)
        {
            if (onMottatMelding != null)
            {
                onMottatMelding(sender, mottattMeldingArgs);
            }
            else
            {
                var shortMsgType = MeldingHelper.ShortenMessageType(mottattMeldingArgs);
                await Console.Out.WriteLineAsync($"📨 Mottatt {shortMsgType}-melding med MeldingId: {mottattMeldingArgs.Melding.MeldingId}, SvarPaMeldingId: {mottattMeldingArgs.Melding.SvarPaMelding}, MeldingType: {mottattMeldingArgs.Melding.MeldingType} og lagrer i listen");
            }
            MottatMeldingArgsList?.Add(mottattMeldingArgs);
            mottattMeldingArgs.SvarSender?.Ack();
        }

        public void SjekkForventetMelding(Guid sendtMeldingsid, string forventetMeldingstype, IEnumerable<MottattMeldingArgs>? mottattMeldingArgsEnumerable = null)
        {
            SjekkForventetMelding(mottattMeldingArgsEnumerable ?? MottatMeldingArgsList, sendtMeldingsid, new []{forventetMeldingstype}, 1);
        }

        public void SjekkForventetMelding(Guid sendtMeldingsid, IEnumerable<string> forventetEnAvMeldingstype,
            int minstForventetAntall, IEnumerable<MottattMeldingArgs>? mottattMeldingArgsEnumerable = null)
        {
            SjekkForventetMelding(mottattMeldingArgsEnumerable, sendtMeldingsid, forventetEnAvMeldingstype, minstForventetAntall);
        }
        public static void SjekkForventetMelding(IEnumerable<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingsid, IEnumerable<string> forventetEnAvMeldingstype, int minstForventetAntall)
        {
            var found = mottattMeldingArgsList
                .Where( mottattMeldingArgs => mottattMeldingArgs.Melding.SvarPaMelding == sendtMeldingsid)
                .Where(mottatMeldingArgs => forventetEnAvMeldingstype.Contains(mottatMeldingArgs.Melding.MeldingType))
                .DistinctBy(m => m.Melding.MeldingId)
                ;
            var feilmeldinger =
                mottattMeldingArgsList.Where(m => FiksArkivMeldingtype.IsFeilmelding(m.Melding.MeldingType)).ToList();
            if (found.Count() < minstForventetAntall)
            {
                foreach (var mottatMeldingArgs in mottattMeldingArgsList)
                {
                    if (FiksArkivMeldingtype.IsFeilmelding(mottatMeldingArgs.Melding.MeldingType))
                    {
                        var feilmelding = HentUtFeilMelding(mottatMeldingArgs);
                        throw new UnexpectedAnswerException($"Uforventet feilmelding mottatt {mottatMeldingArgs.Melding.MeldingType}. Feilmelding: {feilmelding.Feilmelding}. Forventet meldingstypen {string.Join(",", forventetEnAvMeldingstype)}");
                    }

                    var feilmeldingerString = string.Join(Environment.NewLine,
                        feilmeldinger.Select(m => HentUtFeilMelding(m).Feilmelding));
                    throw new UnexpectedAnswerException($"Uforventet melding mottatt av typen {mottatMeldingArgs.Melding.MeldingType}. Forventet meldingstypen {string.Join(", ", forventetEnAvMeldingstype)}.{Environment.NewLine} Feilmeldinger: {feilmeldinger.Count}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine} {feilmeldingerString}");  
                }
            }
            Console.Out.WriteLineAsync($"🎉 Forventet meldingstype {string.Join(", ", forventetEnAvMeldingstype)} mottatt for meldingsid  {sendtMeldingsid}!");
        }

        protected static FeilmeldingBase HentUtFeilMelding(MottattMeldingArgs mottatMeldingArgs)
        {
            var melding = MeldingHelper.GetDecryptedMessagePayload(mottatMeldingArgs).Result;
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
        

        public static MottattMeldingArgs? GetMottattMelding(List<MottattMeldingArgs>? mottattMeldingArgsList, Guid sendtMeldingsid, string forventetMeldingstype)
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

        protected async Task<Saksmappe?> HentSaksmappe(string testSessionId, EksternNoekkel referanseEksternNoekkel)
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
            var mappeHentMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.MappeHent, mappeHentSerialized, null, testSessionId);

            // Vent på 1 respons meldinger 
            VentPaSvar(1, 10);

            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            if( Ikkfunnet(MottatMeldingArgsList, mappeHentMeldingId) )
            {
                return null; 
            }
            
            // Verifiser at man får mappeHentResultat melding
            var mappeHentResultatMelding = GetMottattMelding(MottatMeldingArgsList, mappeHentMeldingId, FiksArkivMeldingtype.MappeHentResultat);

            Assert.IsNotNull(mappeHentResultatMelding);
            
            var mappeHentResultatPayload = await MeldingHelper.GetDecryptedMessagePayload(mappeHentResultatMelding);
            
            // Valider innhold (xml)
            validator.Validate(mappeHentResultatPayload.PayloadAsString);

            var mappeHentResultat = SerializeHelper.DeserializeXml<MappeHentResultat>(mappeHentResultatPayload.PayloadAsString);

            return (Saksmappe)mappeHentResultat.Mappe;
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
            var nySaksmappeMeldingId = await FiksRequestService.Send(MottakerKontoId, FiksArkivMeldingtype.ArkivmeldingOpprett,
                nySaksmappeSerialized, null, testSessionId);

            // Vent på 2 første response meldinger (mottatt og kvittering)
            VentPaSvar(2, 10);
            Assert.True(MottatMeldingArgsList.Count > 0, "Fikk ikke noen meldinger innen timeout");

            // Verifiser at man får mottatt melding
            GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId, FiksArkivMeldingtype.ArkivmeldingOpprettMottatt);

            // Verifiser at man får arkivmeldingKvittering melding
            var arkivmeldingKvitteringMelding = GetMottattMelding(MottatMeldingArgsList, nySaksmappeMeldingId,
                FiksArkivMeldingtype.ArkivmeldingOpprettKvittering);

            var arkivmeldingKvitteringPayload = MeldingHelper.GetDecryptedMessagePayload(arkivmeldingKvitteringMelding).Result;
            Assert.True(arkivmeldingKvitteringPayload.Filename == "arkivmelding-kvittering.xml",
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
            protected static ReferanseTilMappe GenererReferanseTilMappe( string? noekkel = null) => new ()
            {
                ReferanseEksternNoekkel = GenererEksternNoekkel(noekkel)
            };
    }
}
