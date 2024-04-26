namespace fileDriftServer.Hubs;

class PeerRequestMessage
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string NetworkPeerId { get; set; } = string.Empty;
}