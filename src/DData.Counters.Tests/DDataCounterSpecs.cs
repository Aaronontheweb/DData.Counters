using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Remote.Hosting;
using DData.Counters.Actors;
using Xunit.Abstractions;
using static DData.Counters.Actors.DDataKeys;

namespace DData.Counters.Tests;

public class DDataCounterSpecs : TestKit
{
    public DDataCounterSpecs(ITestOutputHelper outputHelper) : base(null, outputHelper){}
    
    [Fact]
    public async Task CounterActorShouldTrackGCounterUpdates()
    {
        // arrange
        await AwaitConditionAsync(() =>
        {
            return Cluster.Get(Sys).State.Members.All(m => m.Status == MemberStatus.Up);
        });
        var selfAddress = Cluster.Get(Sys).SelfAddress;
        
        var replicator = await ActorRegistry.GetAsync<ReplicatorKey>();
        var reader = Props.Create(() => new DDataCounterReader(replicator));

        var readerActorRef = ActorOfAsTestActorRef<DDataCounterReader>(reader);
        var initialState = readerActorRef.UnderlyingActor.GCounter;

        // act
        
        
        // assert
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithRemoting("localhost", 0)
            .WithClustering();
        builder.AddReplicator();
        builder.AddStartup(async (system, registry) =>
        {
            var cluster = Akka.Cluster.Cluster.Get(system);
            var addr = cluster.SelfAddress;
            await cluster.JoinAsync(addr);
        });
    }
}