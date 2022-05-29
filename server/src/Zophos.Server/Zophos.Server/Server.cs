using System.Net;
using System.Net.Sockets;
using FlatBuffers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zophos.Data;
using Zophos.Data.Models.Db;
using Zophos.Server.Helpers;
using Zophos.Server.Models;

namespace Zophos.Server;

public class MessageState
{
    public BaseMessage BaseMessage;

    public EndPoint Remote;

    public Player? Player;
}

public class Server : IHostedService
{
    public readonly IList<ConnectedPlayer> ConnectedPlayers;

    private readonly Socket _socket;

    private Thread _tickThread;

    private const int TickRateMilliseconds = 32;

    private readonly ILogger _logger;
    
    private readonly IPlayerRegistrationService _playerRegistrationService;

    public Server(ILogger<Server> logger, IPlayerRegistrationService playerRegistrationService)
    {
        _logger = logger;
        _playerRegistrationService = playerRegistrationService;

        ConnectedPlayers = new List<ConnectedPlayer>();
        
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
            // TODO: I don't think this lock does quite what I want it to. Need more thread-safety in case of disconnects etc.
            lock (ConnectedPlayers)
            {
                foreach (var destinationClient in ConnectedPlayers)
                {
                    foreach (var subjectClient in ConnectedPlayers)
                    {
                        SendClientPositionToConnectedPlayer(destinationClient, subjectClient);
                    }
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

            var validClientId = Guid.TryParse(message.ClientId, out var clientId);
            var connectedPlayer = validClientId ? ConnectedPlayers.FirstOrDefault(x => x.Player?.Id == clientId) : null;

            if (connectedPlayer == null && message.MessageType != Message.PlayerConnectMessage)
            {
                // This client doesn't exist and isn't trying to connect, bin the request.
                continue;
            }

            var state = new MessageState
            {
                BaseMessage = message,
                Player = connectedPlayer?.Player,
                Remote = remote
            };

            HandleMessage(state);
        }
    }

    private void DropClient(EndPoint remote)
    {
        var client = ConnectedPlayers.FirstOrDefault(x => x.EndPoint == remote);

        if (client == null)
        {
            return;
        }
        
        ConnectedPlayers.Remove(client);
    }
    
    private void HandleMessage(MessageState state)
    {
        // TODO: this shouldn't be in the main server class. Events?
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
                SendMessageToAllConnectedPlayers(state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state.BaseMessage.MessageType));
        }
    }

    private void PlayerConnect(MessageState state)
    {
        var player = _playerRegistrationService.RegisterPlayer("Me");

        var connectedPlayer = new ConnectedPlayer
        {
            EndPoint = state.Remote,
            Player = player.Player
        };

        ConnectedPlayers.Add(connectedPlayer);
        SendPlayerIdMessage(connectedPlayer);
    }

    private void SetName(MessageState state)
    {
        // TODO: an attribute to mark that this requires a client?
        if (state.Player == null)
        {
            return;
        }
        
        var setNameMessage = state.BaseMessage.MessageAsSetNameMessage();
        state.Player.Name = setNameMessage.Name;
    }

    private void UpdatePosition(MessageState state)
    {
        // TODO: an attribute to mark that this requires a client?
        if (state.Player == null)
        {
            return;
        }

        var updatePositionMessage = state.BaseMessage.MessageAsUpdatePositionMessage();
        state.Player.X = updatePositionMessage.X;
        state.Player.Y = updatePositionMessage.Y;
    }

    private void SendPlayerIdMessage(ConnectedPlayer player)
    {
        if (player.Player == null)
        {
            return;
        }

        var byteBuffer = MessageBuilder.BuildPlayerIdMessage(player.Player);
        _socket.SendToAsync(byteBuffer, SocketFlags.None, player.EndPoint);
    }

    private void SendClientPositionToConnectedPlayer(ConnectedPlayer subjectPlayer, ConnectedPlayer destinationPlayer)
    {
        if (subjectPlayer.Player == null || destinationPlayer.Player == null)
        {
            return;
        }

        var byteBuffer = MessageBuilder.BuildUpdatePositionMessage(subjectPlayer.Player);
        _socket.SendToAsync(byteBuffer, SocketFlags.None, destinationPlayer.EndPoint);
    }

    private void SendMessageToAllConnectedPlayers(MessageState state)
    {
        var chatMessage = state.BaseMessage.MessageAsChatMessage();
        var byteBuffer = MessageBuilder.BuildChatMessage(state.Player, chatMessage.Contents);

        foreach (var client in ConnectedPlayers)
        {
            _socket.SendTo(byteBuffer, byteBuffer.Length, SocketFlags.None, client.EndPoint);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting...");
        Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping...");
        return Task.CompletedTask;
    }
}