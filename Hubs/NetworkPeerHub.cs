
using System.Net.NetworkInformation;
using fileDriftServer.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace fileDriftServer.Hubs;

class NetworkPeerHub : Hub
{
    public void JoinRoom()
    {
        var remoteAddress = GetRemoteAddress();
        Clients.Group(remoteAddress).SendAsync("JoinRoomMessage", new JoinRoomMessage() { From = Context.ConnectionId }); // TODO: We can send a username, profile image etc..
        Groups.AddToGroupAsync(Context.ConnectionId, remoteAddress);
    }

    public void RequestPeerConnection(string connectionId, string networkPeerId)
    {
        Clients.Client(connectionId).SendAsync("PeerRequestMessage", new PeerRequestMessage() { From = Context.ConnectionId, To = connectionId, NetworkPeerId = networkPeerId }); // TODO: send a class instead of string.
    }

    public void LeaveRoom()
    {
        var remoteAddress = GetRemoteAddress();
        Groups.RemoveFromGroupAsync(Context.ConnectionId, remoteAddress);
        Clients.Group(remoteAddress).SendAsync("LeaveRoomMessage", new LeaveRoomMessage() { From = Context.ConnectionId });
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        LeaveRoom();
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

}