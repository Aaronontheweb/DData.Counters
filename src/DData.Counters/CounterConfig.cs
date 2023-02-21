using Akka.Actor;
using Akka.DistributedData;
using Akka.Hosting;
using DData.Counters.Actors;

namespace DData.Counters;

public static class CounterConfig
{
    public static AkkaConfigurationBuilder AddReplicator(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry) =>
        {
            var replicator = DistributedData.Get(system).Replicator;
            registry.Register<ReplicatorKey>(replicator);
        });
    }

    public static AkkaConfigurationBuilder AddCounterReaderActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry) =>
        {
            // add DDataCounterReader actor
            var replicator = registry.Get<ReplicatorKey>();
            var reader = system.ActorOf(Props.Create(() => new DDataCounterReader(replicator)), "reader");
            registry.Register<DDataCounterReader>(reader);
        });
    }

    public static AkkaConfigurationBuilder AddCounterWriterActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry) =>
        {
            // add DDataWriter actor
            var replicator = registry.Get<ReplicatorKey>();
            var writer = system.ActorOf(Props.Create(() => new DDataWriter(replicator)), "writer");
            registry.Register<DDataWriter>(writer);
        });
    }
}