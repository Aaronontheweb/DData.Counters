using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.DistributedData;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Remote.Hosting;
using DData.Counters.Actors;
using FluentAssertions;
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
        var selfAddress = Cluster.Get(Sys).SelfUniqueAddress;
        
        var replicator = await ActorRegistry.GetAsync<ReplicatorKey>();
        var reader = Props.Create(() => new DDataCounterReader(replicator));

        var readerActorRef = ActorOfAsTestActorRef<DDataCounterReader>(reader);
        var initialState = readerActorRef.UnderlyingActor.GCounter;

        // act
        foreach (var i in Enumerable.Range(0, 2))
        {
            var update1 = Dsl.Update(CounterKey, GCounter.Empty, WriteLocal.Instance,
                g => g.Increment(selfAddress));
            await replicator.Ask<UpdateSuccess>(update1);
        }

        // assert
        await AwaitAssertAsync(() =>
        {
            readerActorRef.UnderlyingActor.GCounter.State.TryGetValue(selfAddress, out var value);
            value.Should().Be(2);
        });
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