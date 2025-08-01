// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.DistributedData;
using Akka.Hosting;
using Akka.Remote.Hosting;
using DData.Counters;
using DData.Counters.Actors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";


var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
        .AddJsonFile("appsettings.json"))
        //.AddJsonFile($"appsettings.{environment}.json"))
        .ConfigureServices((context, collection) =>
    {
        var akkaSection = context.Configuration.GetSection("Akka");

        // maps to environment variable Akka__ClusterIp
        var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

        // maps to environment variable Akka__ClusterPort
        var port = akkaSection.GetValue<int>("ClusterPort", 0);

        var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new[] { "akka.tcp://DDataCounter@localhost:8110" });
        
        collection.AddAkka("DDataCounter", configurationBuilder =>
        {
            // enable akka.remote and akka.cluster
            configurationBuilder
                .AddHocon(@"akka.cluster.distributed-data{
                        role = ddata
                        gossip-interval = 10 s
                    }", HoconAddMode.Prepend)
                .WithRemoting(hostName, port)
                .WithClustering(new ClusterOptions(){ Roles = new []{"ddata"}, SeedNodes = seeds})
                .AddReplicator()
                .AddCounterReaderActor()
                .AddCounterWriterActor()
                .AddLwwDictionaryActor();
        });
    })
    .Build();

await builder.RunAsync();;