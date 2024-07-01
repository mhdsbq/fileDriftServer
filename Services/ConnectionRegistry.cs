using System.Collections.Concurrent;

class ConnectionRegistry
{
    private IDictionary<string, string> PeerIdToSignalRIdMap = new ConcurrentDictionary<string, string>();
    private IDictionary<string, string> SignalRIdToPeerIdMap = new ConcurrentDictionary<string, string>();
    private readonly ILogger<ConnectionRegistry> _logger;

    public ConnectionRegistry(ILogger<ConnectionRegistry> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetPeerId(string signalRId)
    {
        return SignalRIdToPeerIdMap[signalRId];
    }

    public string GetSignalRId(string peerId)
    {
        return PeerIdToSignalRIdMap[peerId];
    }

    public void Add(string peerId, string signalRId)
    {
        if (!PeerIdToSignalRIdMap.TryAdd(peerId, signalRId))
        {
            _logger.LogError("[REG] Peer id already exist in registry. PeerId: {0}", peerId);
        }

        if (!SignalRIdToPeerIdMap.TryAdd(signalRId, peerId))
        {
            _logger.LogError("[REG] SignalR id already exist in registry. SignalR Id: {0}", signalRId);
        }
    }

    public void RemoveByPeerId(string peerId)
    {
        if (peerId is null)
        {
            throw new ArgumentNullException(nameof(peerId));
        }

        if (PeerIdToSignalRIdMap.TryGetValue(peerId, out var signalRId))
        {
            SignalRIdToPeerIdMap.Remove(signalRId);
        }

        PeerIdToSignalRIdMap.Remove(peerId);
    }

    public void RemoveBySignalRId(string signalRId)
    {
        if (signalRId is null)
        {
            throw new ArgumentNullException(nameof(signalRId));
        }

        if (SignalRIdToPeerIdMap.TryGetValue(signalRId, out var peerId))
        {
            PeerIdToSignalRIdMap.Remove(peerId);
        }

        SignalRIdToPeerIdMap.Remove(signalRId);
    }
}