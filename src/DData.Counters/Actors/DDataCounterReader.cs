using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData;
using Akka.Event;
using static DData.Counters.Actors.DDataKeys;

namespace DData.Counters.Actors;

public sealed class DDataCounterReader : ReceiveActor
{
    // add logger, cluster, and reference to replicator actor
    private readonly ILoggingAdapter _logger = Context.GetLogger();
    private readonly IActorRef _replicator;
    
    public GCounter GCounter = GCounter.Empty;

    public DDataCounterReader(IActorRef replicator)
    {
        _replicator = replicator;
        
        // receive updates for CounterKey from replicator
        Receive<Changed>(changed => changed.Key.Equals(CounterKey), update =>
        {
            var counter = update.Get(CounterKey);
           GCounter = GCounter.Merge(counter);
            _logger.Info("Counter changed to {0}", counter);
            // print counter values from all nodes
            foreach (var i in counter.State)
            {
                _logger.Info($"Counter value on node {i.Key} is {i.Value}");
            }
            
            _logger.Info($"Aggregate counter value is {GCounter.Value}");
        });
    }

    protected override void PreStart()
    {
        // subscribe to changes in the counter
        _replicator.Tell(Dsl.Subscribe(CounterKey, Self));
    }
}