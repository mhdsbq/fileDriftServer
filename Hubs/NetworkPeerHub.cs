
using System.Text.RegularExpressions;
using fileDriftServer.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
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

        // Note     / TODO: Group except is used to remedy re-join request when page re renders. Need to update front end code as well. Maybe move all orchestration to a root service from appComponent(The issue is made by auto re-rendering done by browser after sleep.)
        Clients.GroupExcept(remoteAddress, [Context.ConnectionId]).SendAsync("JoinRoomMessage", new JoinRoomMessage() { From = dataConnectionId }); // TODO: We can send a username, profile image etc..
        Groups.AddToGroupAsync(Context.ConnectionId, remoteAddress);

        _connectionRegistry.Add(dataConnectionId, Context.ConnectionId);
        _logger.LogInformation($"[HUB] Connection with id {dataConnectionId} has joined {remoteAddress}");
        _logger.LogInformation("[HUB] Info has been added to registry. PeerId: {0}, SignalRId:{1}", dataConnectionId, Context.ConnectionId);
    }

    public void LeaveRoom(string dataConnectionId)
    {
        var remoteAddress = GetRemoteAddress();
        Groups.RemoveFromGroupAsync(Context.ConnectionId, remoteAddress);
        Clients.Group(remoteAddress).SendAsync("LeaveRoomMessage", new LeaveRoomMessage() { From = dataConnectionId });
        _logger.LogInformation($"[HUB] Connection with id {dataConnectionId} has left {remoteAddress}");

        _connectionRegistry.RemoveByPeerId(dataConnectionId);
    }

    public void FileSendRequest(FileSendRequest fileSendRequest)
    {
        BlockSpoofing(fileSendRequest.FileSenderId);

        if (fileSendRequest is null || fileSendRequest.FileSenderId is null || fileSendRequest.FileReceiverId is null || fileSendRequest.FileInfo is null)
        {
            throw new ArgumentException(nameof(fileSendRequest));
        }

        // TODO: make sure both are in same network.

        var receiverConnectionId = _connectionRegistry.GetSignalRId(fileSendRequest.FileReceiverId);
        if (receiverConnectionId is null)
        {
            throw new InvalidOperationException(); //TODO: Use a custom exception, also exception handler middleware
        }

        Clients.Client(receiverConnectionId).SendAsync("FileSendRequest", fileSendRequest);
    }

    /// <summary>
    /// File accept or reject message. Send by the file receiver back to the file sender.
    /// </summary>
    public void FileSendResponse(FileSendResponse fileSendResponse)
    {
        BlockSpoofing(fileSendResponse.FileReceiverId);

        if (fileSendResponse is null || fileSendResponse.FileSenderId is null || fileSendResponse.FileReceiverId is null)
        {
            throw new ArgumentNullException(nameof(fileSendResponse));
        }

        // here message is sent to the file sender from file receiver.
        var messageReceiverConnectionId = _connectionRegistry.GetSignalRId(fileSendResponse.FileSenderId);
        if (messageReceiverConnectionId is null)
        {
            throw new InvalidOperationException();
        }

        Clients.Client(messageReceiverConnectionId).SendAsync("FileSendResponse", fileSendResponse);
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