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

    private Thread _tickThread;

    private const int TickRateMilliseconds = 32;

    public Server()
    {
        Clients = new List<Client>();
        
        var ip = new IPEndPoint(IPAddress.Any, 22122);
        _socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(ip);

        var threadStart = new ThreadStart(Tick);
        _tickThread = new Thread(threadStart);
        _tickThread.Start();
    }

    private void Tick()
    {
        while (true)
        {
            foreach (var destinationClient in Clients)
            {
                foreach (var subjectClient in Clients)
                {
                    SendClientPositionToClient(destinationClient, subjectClient);
                }
            }
            
            Thread.Sleep(TickRateMilliseconds);
        }
    }

    public void Start()
    {
        var sender = new IPEndPoint(IPAddress.Any, 0);
        var remote = (EndPoint)sender;

        while (true)
        {
            var receivedData = new byte[1024];
            try
            {
                _socket.ReceiveFrom(receivedData, ref remote);
            }
            catch (SocketException)
            {
                DropClient(remote);
                continue;
            }

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

            HandleMessage(state);
        }
    }

    private void DropClient(EndPoint remote)
    {
        var client = Clients.FirstOrDefault(x => x.EndPoint == remote);

        if (client == null)
        {
            return;
        }
        
        Clients.Remove(client);
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
                SendMessageToAllClients(state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state.BaseMessage.MessageType));
        }
    }

    private void PlayerConnect(MessageState state)
    {
        // TODO: we shouldn't need to do this: if the client was already connected it would be in the state object.
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
        // TODO: an attribute to mark that this requires a client?
        if (state.Client == null)
        {
            return;
        }
        
        var setNameMessage = state.BaseMessage.MessageAsSetNameMessage();
        state.Client.Name = setNameMessage.Name;
    }

    private void UpdatePosition(MessageState state)
    {
        // TODO: an attribute to mark that this requires a client?
        if (state.Client == null)
        {
            return;
        }

        var updatePositionMessage = state.BaseMessage.MessageAsUpdatePositionMessage();
        state.Client.X = updatePositionMessage.X;
        state.Client.Y = updatePositionMessage.Y;
    }

    private void SendClientPositionToClient(Client subjectClient, Client destinationClient)
    {
        var builder = new FlatBufferBuilder(1024);
        
        UpdatePositionMessage.StartUpdatePositionMessage(builder);
        UpdatePositionMessage.AddX(builder, subjectClient.X);
        UpdatePositionMessage.AddY(builder, subjectClient.Y);
        var buildUpdatePositionMessage = UpdatePositionMessage.EndUpdatePositionMessage(builder);
        
        var buildClientId = builder.CreateString(subjectClient.ClientId);
        BaseMessage.StartBaseMessage(builder);
        BaseMessage.AddMessageType(builder, Message.UpdatePositionMessage);
        BaseMessage.AddMessage(builder, buildUpdatePositionMessage.Value);
        BaseMessage.AddClientId(builder, buildClientId);
        var baseMessage = BaseMessage.EndBaseMessage(builder);

        builder.Finish(baseMessage.Value);

        var byteBuffer = builder.SizedByteArray();
        _socket.SendToAsync(byteBuffer, SocketFlags.None, destinationClient.EndPoint);
    }

    private void SendMessageToAllClients(MessageState state)
    {
        var chatMessage = state.BaseMessage.MessageAsChatMessage();
        
        var builder = new FlatBufferBuilder(1024);
        
        var buildContents = builder.CreateString(chatMessage.Contents);
        var buildSourceClientId = builder.CreateString(state.Client?.ClientId);
        var buildDestinationSourceId = builder.CreateString(string.Empty);
        ChatMessage.StartChatMessage(builder);
        ChatMessage.AddContents(builder, buildContents);
        ChatMessage.AddSourceClientId(builder, buildSourceClientId);
        ChatMessage.AddDestinationClientId(builder, buildDestinationSourceId);
        var buildChatMessage = ChatMessage.EndChatMessage(builder);
        
        var buildClientId = builder.CreateString(state.Client?.ClientId);
        BaseMessage.StartBaseMessage(builder);
        BaseMessage.AddMessageType(builder, Message.ChatMessage);
        BaseMessage.AddMessage(builder, buildChatMessage.Value);
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