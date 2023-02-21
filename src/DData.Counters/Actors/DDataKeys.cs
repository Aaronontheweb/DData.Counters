using Akka.DistributedData;

namespace DData.Counters.Actors;

public class ReplicatorKey{}

public static class DDataKeys
{
    public static readonly GCounterKey CounterKey = new("mycounter");
}