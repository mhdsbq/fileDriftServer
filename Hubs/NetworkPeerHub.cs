
using System.Text.Json;
using fileDriftServer.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace fileDriftServer.Hubs;

class NetworkPeerHub : Hub
{
    private readonly ConnectionRegistry _connectionRegistry;
    private readonly ILogger<NetworkPeerHub> _logger;
    public NetworkPeerHub(ConnectionRegistry connectionRegistry, ILogger<NetworkPeerHub> logger)
    {
        _connectionRegistry = connectionRegistry ?? throw new ArgumentNullException(nameof(connectionRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void JoinRoom(string dataConnectionId)
    {
        var remoteAddress = GetRemoteAddress();
        Clients.GroupExcept(remoteAddress, [Context.ConnectionId]).SendAsync("JoinRoomMessage", new JoinRoomMessage() { From = dataConnectionId }); // TODO: We can send a username, profile image etc..
        Groups.AddToGroupAsync(Context.ConnectionId, remoteAddress);

        _connectionRegistry.Add(dataConnectionId, Context.ConnectionId);
        _logger.LogInformation($"[HUB] Connection with id {dataConnectionId} has joined {remoteAddress}");
        _logger.LogInformation("[HUB] Info has been added to registry. PeerId: {0}, SignalRId:{1}", dataConnectionId, Context.ConnectionId);
    }

    public void SendSignalMessage(object signalMessageObj)
    {
        var signalMessage = JsonSerializer.Deserialize<SignalMessage>(signalMessageObj.ToString()!, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
        BlockSpoofing(signalMessage.SenderId);
        // TODO: Check if user is in the same network

        signalMessage.SentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Clients.Client(_connectionRegistry.GetSignalRId(signalMessage.ReceiverId)).SendAsync("SignalMessage", signalMessageObj);
        _logger.LogInformation($"[HUB] Sent a Signal Message of type {signalMessage.MessageType} from {signalMessage.SenderId} to {signalMessage.ReceiverId}");
    }

    public void LeaveRoom(string dataConnectionId)
    {
        var remoteAddress = GetRemoteAddress();
        Groups.RemoveFromGroupAsync(Context.ConnectionId, remoteAddress);
        Clients.Group(remoteAddress).SendAsync("LeaveRoomMessage", new LeaveRoomMessage() { From = dataConnectionId });
        _logger.LogInformation($"[HUB] Connection with id {dataConnectionId} has left {remoteAddress}");

        _connectionRegistry.RemoveByPeerId(dataConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // LeaveRoom(); handle this - resolve connectionId using context connectionId
        _logger.LogInformation($"[HUB] A connection has been disconnected from the server.");
        return base.OnDisconnectedAsync(exception);

    }

    private string GetRemoteAddress()
    {
        var httpConnection = Context.Features.Get<IHttpConnectionFeature>();
        if (httpConnection is null || httpConnection.RemoteIpAddress is null) // Need to check x forword for header during deployment.
        {
            throw new Exception("Ip address should be visible"); // Todo: Handle this case properly.
        }
        return httpConnection.RemoteIpAddress.ToString();
    }

    /// <summary>
    /// Block spoofing attempts done by providing someone elses peerId.
    /// </summery>
    private void BlockSpoofing(string providedPeerId)
    {
        var originalPeerId = _connectionRegistry.GetPeerId(Context.ConnectionId);
        if (providedPeerId != originalPeerId)
        {
            this._logger.LogCritical("[HUB] Spoofing attempt detected!");
            throw new InvalidOperationException("Invalid request.");
        }
    }
}