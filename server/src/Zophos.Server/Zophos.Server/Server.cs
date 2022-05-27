using System.Net;
using System.Net.Sockets;
using FlatBuffers;
using Zophos.Data;

namespace Zophos.Server;

public class MessageState
{
    public BaseMessage BaseMessage;

    public EndPoint Remote;

    public Client Client;
}

public class Server
{
    public readonly IList<Client> Clients;

    private readonly Socket _socket;

    public Server()
    {
        Clients = new List<Client>();
        
        var ip = new IPEndPoint(IPAddress.Any, 22122);
        _socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(ip);
    }

    public void Start()
    {
        var sender = new IPEndPoint(IPAddress.Any, 0);
        var remote = (EndPoint)sender;

        while (true)
        {
            var receivedData = new byte[1024];
            _socket.ReceiveFrom(receivedData, ref remote);
            var byteBuffer = new ByteBuffer(receivedData);
            var message = BaseMessage.GetRootAsBaseMessage(byteBuffer);
            
            var client = Clients.FirstOrDefault(x => x.ClientId == message.ClientId);
            
            if (client == null && message.MessageType != Message.PlayerConnectMessage)
            {
                // This client doesn't exist and isn't trying to connect, bin the request.
                continue;
            }
            
            var state = new MessageState
            {
                BaseMessage = message,
                Client = client,
                Remote = remote
            };
            
            if (message.MessageType == Message.NONE)
            {
                continue;
            }

            HandleMessage(state);
        }
    }
    
    private void HandleMessage(MessageState state)
    {
        switch (state.BaseMessage.MessageType)
        {
            case Message.NONE:
                break;
            case Message.SetNameMessage:
                SetName(state);
                break;
            case Message.UpdatePositionMessage:
                UpdatePosition(state);
                break;
            case Message.PlayerConnectMessage:
                PlayerConnect(state);
                break;
            case Message.ChatMessage:
                var chatMessage = state.BaseMessage.MessageAsChatMessage();
                SendMessageToAllClients(chatMessage.Contents);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state.BaseMessage.MessageType));
        }
    }

    private void PlayerConnect(MessageState state)
    {
        if (Clients.FirstOrDefault(x => x.ClientId == state.BaseMessage.ClientId) != null)
        {
            // Player is already connected.
            return;
        }

        Clients.Add(new Client(state.BaseMessage.ClientId)
        {
            EndPoint = state.Remote
        });
    }

    private void SetName(MessageState state)
    {
        if (state.Client == null)
        {
            return;
        }
        
        var setNameMessage = state.BaseMessage.MessageAsSetNameMessage();
        state.Client.Name = setNameMessage.Name;
    }

    private void UpdatePosition(MessageState state)
    {
        if (state.Client == null)
        {
            return;
        }

        var updatePositionMessage = state.BaseMessage.MessageAsUpdatePositionMessage();
        state.Client.X = updatePositionMessage.X;
        state.Client.Y = updatePositionMessage.Y;
    }

    private void SendMessageToAllClients(string message)
    {
        var builder = new FlatBufferBuilder(1024);
        
        var buildContents = builder.CreateString(message);
        ChatMessage.StartChatMessage(builder);
        ChatMessage.AddContents(builder, buildContents);
        ChatMessage.AddSourceClientId(builder, 6969);
        ChatMessage.AddDestinationClientId(builder, 420);
        var chatMessage = ChatMessage.EndChatMessage(builder);
        
        var buildClientId = builder.CreateString("someone");
        BaseMessage.StartBaseMessage(builder);
        BaseMessage.AddMessageType(builder, Message.ChatMessage);
        BaseMessage.AddMessage(builder, chatMessage.Value);
        BaseMessage.AddClientId(builder, buildClientId);
        var baseMessage = BaseMessage.EndBaseMessage(builder);

        builder.Finish(baseMessage.Value);

        var byteBuffer = builder.SizedByteArray();

        foreach (var client in Clients)
        {
            _socket.SendTo(byteBuffer, byteBuffer.Length, SocketFlags.None, client.EndPoint);
        }
    }
}