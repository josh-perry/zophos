using System.Net;
using System.Net.Sockets;
using FlatBuffers;
using Zophos.Data;

namespace Zophos.Server;

public class Server
{
    public readonly IList<Client> Clients;

    public Server()
    {
        Clients = new List<Client>();
    }

    public void Start()
    {
        var ip = new IPEndPoint(IPAddress.Any, 22122);
        var socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);

        socket.Bind(ip);

        while (true)
        {
            var receivedData = new byte[1024];
            var byteBuffer = new ByteBuffer(receivedData);
            var message = BaseMessage.GetRootAsBaseMessage(byteBuffer);
            
            // TODO: something better than this!
            if (message.MessageType == Message.NONE)
            {
                continue;
            }
            
            HandleMessage(message);
        }
    }
    
    private void HandleMessage(BaseMessage message)
    {
        var client = Clients.FirstOrDefault(x => x.ClientId == message.ClientId);
        if (client == null && message.MessageType != Message.PlayerConnectMessage)
        {
            // This client doesn't exist and isn't trying to connect, bin the request.
            return;
        }
        
        switch (message.MessageType)
        {
            case Message.NONE:
                break;
            case Message.SetNameMessage:
                SetName(message);
                break;
            case Message.UpdatePositionMessage:
                UpdatePosition(message);
                break;
            case Message.PlayerConnectMessage:
                PlayerConnect(message);
                break;
            case Message.HeartbeatMessage:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message.MessageType));
        }
    }

    private void PlayerConnect(BaseMessage message)
    {
        if (Clients.FirstOrDefault(x => x.ClientId == message.ClientId) != null)
        {
            // Player is already connected.
            return;
        }

        Clients.Add(new Client(message.ClientId));
    }

    private void SetName(BaseMessage message)
    {
        var client = Clients.FirstOrDefault(x => x.ClientId == message.ClientId);
        if (client == null)
        {
            return;
        }
        
        var setNameMessage = message.MessageAsSetNameMessage();
        client.Name = setNameMessage.Name;
    }

    private void UpdatePosition(BaseMessage message)
    {
        var client = Clients.FirstOrDefault(x => x.ClientId == message.ClientId);
        if (client == null)
        {
            return;
        }

        var updatePositionMessage = message.MessageAsUpdatePositionMessage();
        client.X = updatePositionMessage.X;
        client.Y = updatePositionMessage.Y;
    }
}