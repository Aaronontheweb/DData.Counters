using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData;
using Akka.Event;
using static DData.Counters.Actors.DDataKeys;

namespace DData.Counters.Actors;

public sealed class DDataCounterWriter : ReceiveActor, IWithTimers
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _replicator;
    private readonly Cluster _cluster = Cluster.Get(Context.System);

    private sealed class Increment
    {
        // make singleton
        private Increment()
        {
        }

        public static readonly Increment Instance = new();
    }

    public DDataCounterWriter(IActorRef replicator)
    {
        _replicator = replicator;

        Receive<Increment>(_ => IncrementCounter());
        Receive<UpdateSuccess>(msg => msg.Key.Equals(CounterKey), success =>
        {
          _log.Info("Successfully updated counter [{0}]", success.Key);
        });
    }

    private void IncrementCounter()
    {
        var node = _cluster.SelfMember.UniqueAddress;
        var update = Dsl.Update(CounterKey, GCounter.Empty, WriteLocal.Instance,
            g => g.Increment(node));
        _replicator.Tell(update);
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("tick", Increment.Instance, TimeSpan.FromSeconds(2));
    }

    public ITimerScheduler Timers { get; set; }
}