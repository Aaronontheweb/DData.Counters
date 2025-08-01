using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData;
using Akka.Event;
using static DData.Counters.Actors.DDataKeys;

namespace DData.Counters.Actors;

public sealed class DDataDictionaryWriter : UntypedActor, IWithTimers
{
    private sealed class SetItem
    {
        // make singleton
        private SetItem()
        {
        }

        public static readonly SetItem Instance = new();
    }
    
    private readonly string[] _keys = {
        "key1", "key2", "key3", "key4", "key5"
    };
    
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _replicator;
    private readonly Cluster _cluster = Cluster.Get(Context.System);
    
    private LWWDictionary<string, string> _dictionary = LWWDictionary<string, string>.Empty;

    public DDataDictionaryWriter(IActorRef replicator)
    {
        _replicator = replicator;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Changed changed when changed.Key.Equals(DictionaryKey):
                // Handle changes to the dictionary
                var updatedDict = changed.Get(DictionaryKey);
                _dictionary = updatedDict;
                _log.Info("Dictionary updated: {0}", updatedDict);
                
                // Log the current state of the dictionary
                foreach (var item in _dictionary)
                {
                    _log.Info($"Key: {item.Key}, Value: {item.Value}");
                }
                break;
            case SetItem:
                // Set item in the dictionary
                var key = RandomKey();
                var value = Random.Shared.Next(0, 100).ToString();
                var update = Dsl.Update(DictionaryKey, _dictionary, WriteLocal.Instance,
                    dict => dict.SetItem(_cluster.SelfUniqueAddress, key, value));
                _replicator.Tell(update);
                _log.Info("Set item in dictionary: {0} = {1}", key, value);
                break;
        }
    }
    
    private string RandomKey()
    {
        // Return a random key from the predefined keys
        return _keys[Random.Shared.Next(_keys.Length)];
    }

    protected override void PreStart()
    {
        // Start a periodic timer to add items to the dictionary
        Timers.StartPeriodicTimer("addItem", SetItem.Instance, TimeSpan.FromSeconds(2));
        _log.Info("DDataDictionaryWriter started and timer initialized.");
        
        // Subscribe to changes in the dictionary
        _replicator.Tell(Dsl.Subscribe(DictionaryKey, Self));
        
        // pre-populate the dictionary with some initial values
        var initialUpdate = Dsl.Update(DictionaryKey, LWWDictionary<string, string>.Empty, WriteLocal.Instance,
            dict => dict.SetItem(_cluster.SelfUniqueAddress, RandomKey(), Random.Shared.Next(0,100).ToString()));
        _replicator.Tell(initialUpdate);
    }

    public ITimerScheduler Timers { get; set; }
}