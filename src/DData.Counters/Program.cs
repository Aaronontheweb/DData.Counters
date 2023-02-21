﻿// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.DistributedData;
using Akka.Hosting;
using Akka.Remote.Hosting;
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

        var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new[] { "akka.tcp://DDataCounter@localhost:8110" })
            .Select(Address.Parse)
            .ToArray();
        
        collection.AddAkka("DDataCounter", configurationBuilder =>
        {
            // enable akka.remote and akka.cluster
            configurationBuilder
                .AddHocon(@"akka.cluster.distributed-data{
                        role = ddata
                        gossip-interval = 10 s
                    }", HoconAddMode.Prepend)
                .WithRemoting("localhost", 0)
                .WithClustering(new ClusterOptions(){ Roles = new []{"ddata"}, SeedNodes = seeds})
                .WithActors((system, registry) =>
                {
                    var replicator = DistributedData.Get(system).Replicator;
                    registry.Register<ReplicatorKey>(replicator);
                })
                .WithActors((system, registry) =>
                {
                    // add DDataCounterReader actor
                    var replicator = registry.Get<ReplicatorKey>();
                    var reader = system.ActorOf(Props.Create(() => new DDataCounterReader(replicator)), "reader");
                    registry.Register<DDataCounterReader>(reader);
                })
                .WithActors((system, registry) =>
                {
                    // add DDataWriter actor
                    var replicator = registry.Get<ReplicatorKey>();
                    var writer = system.ActorOf(Props.Create(() => new DDataWriter(replicator)), "writer");
                    registry.Register<DDataWriter>(writer);
                });
        });
    })
    .Build();

await builder.RunAsync();;