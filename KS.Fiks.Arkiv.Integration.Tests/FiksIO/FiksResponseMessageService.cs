using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KS.Fiks.Arkiv.Integration.Tests.FiksIO
{
    public class FiksResponseMessageService : BackgroundService
    {
        private static IFiksIOClient _client;
        public static List<MottattMeldingArgs> MottatMeldingArgsList;
        private FiksIOConfiguration _fiksIoConfiguration;
        private FiksIOClient Client { get; set; }

        public FiksResponseMessageService(IConfigurationRoot fiksIoConfig)
        {
            MottatMeldingArgsList = new List<MottattMeldingArgs>();
            _fiksIoConfiguration = FiksIOConfigurationBuilder.CreateFiksIOConfiguration(fiksIoConfig);
            Initialization = InitializeAsync();
        }

        private Task Initialization { get; set; }

        private async Task InitializeAsync()
        {
            Client = await FiksIOClient.CreateAsync(_fiksIoConfiguration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Console.Out.WriteLineAsync("Starter subscription - ExectueAsync");
            stoppingToken.ThrowIfCancellationRequested();
            await _client.NewSubscriptionAsync(OnMottattMelding);   
        }

        private static async Task OnMottattMelding(MottattMeldingArgs meldingArgs)
        {
            await Console.Out.WriteLineAsync($"Henter melding med MeldingId: {meldingArgs.Melding.MeldingId}, SvarPaMeldingId: {meldingArgs.Melding.SvarPaMelding}, MeldingType: {meldingArgs.Melding.MeldingType}");

            await Console.Out.WriteLineAsync($"MeldingType: {meldingArgs.Melding.MeldingType}");
            
            MottatMeldingArgsList.Add(meldingArgs);
            
            await meldingArgs.SvarSender.AckAsync();
        }

        public List<MottattMeldingArgs> GetMottatMeldingArgsList()
        {
            return MottatMeldingArgsList;
        }
    }
}
