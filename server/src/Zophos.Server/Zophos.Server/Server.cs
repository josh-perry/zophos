using System.Net;
using System.Net.Sockets;
using FlatBuffers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zophos.Data;
using Zophos.Data.Repositories.Contracts;
using Zophos.Server.Helpers;
using Zophos.Server.Models;

namespace Zophos.Server;

public class Server : IHostedService
{
    public readonly IList<ConnectedPlayer> ConnectedPlayers;

    private readonly Socket _socket;

    private const int TickRateMilliseconds = 32;

    private readonly ILogger _logger;
    
    private readonly IPlayerRegistrationService _playerRegistrationService;
    
    private readonly IMessageNotifier _messageNotifier;
    
    private readonly IPlayerRepository _playerRepository;

    public Server(ILogger<Server> logger,
        IPlayerRegistrationService playerRegistrationService,
        IMessageNotifier messageNotifier,
        IPlayerRepository playerRepository)
    {
        _logger = logger;
        _playerRegistrationService = playerRegistrationService;
        _messageNotifier = messageNotifier;
        _playerRepository = playerRepository;

        ConnectedPlayers = new List<ConnectedPlayer>();
        
        var ip = new IPEndPoint(IPAddress.Any, 22122);
        _socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(ip);

        var threadStart = new ThreadStart(Tick);
        var tickThread = new Thread(threadStart);
        tickThread.Start();
        
        new Thread(SaveTick).Start();
        
        InitializeMessageEventHandlers();
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting...");

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

            _messageNotifier.MessageReceived(state);
        }
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping...");
        return Task.CompletedTask;
    }
    
    private void InitializeMessageEventHandlers()
    {
        _messageNotifier.AddHandler(Message.SetNameMessage, SetName);
        _messageNotifier.AddHandler(Message.ChatMessage, SendChatMessageToAllConnectedPlayers);
        _messageNotifier.AddHandler(Message.PlayerConnectMessage, PlayerConnect);
        _messageNotifier.AddHandler(Message.UpdatePositionMessage, UpdatePosition);
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

    private void SaveTick()
    {
        // HACK: lol
        while (true)
        {
            _playerRepository.Save(ConnectedPlayers. Select(x => x.Player)!);
            Thread.Sleep(1000);
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
    
    private void PlayerConnect(MessageState state)
    {
        var playerConnectMessage = state.BaseMessage.MessageAsPlayerConnectMessage();
        var player = _playerRepository.GetPlayerByName(playerConnectMessage.Name);

        if (player == null)
        {
            var newRegistration = _playerRegistrationService.RegisterPlayer(playerConnectMessage.Name);

            if (newRegistration.RegistrationStatus == RegistrationStatus.Failed)
            {
                _logger.LogError("Failed to register player!");
                return;
            }

            player = newRegistration.Player;
        }

        var connectedPlayer = new ConnectedPlayer
        {
            EndPoint = state.Remote,
            Player = player
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

        SendToOne(player, MessageBuilder.BuildPlayerIdMessage(player.Player));
    }

    private void SendClientPositionToConnectedPlayer(ConnectedPlayer subjectPlayer, ConnectedPlayer destinationPlayer)
    {
        if (subjectPlayer.Player == null || destinationPlayer.Player == null)
        {
            return;
        }

        SendToOne(destinationPlayer, MessageBuilder.BuildUpdatePositionMessage(subjectPlayer.Player));
    }

    private void SendChatMessageToAllConnectedPlayers(MessageState state)
    {
        var chatMessage = state.BaseMessage.MessageAsChatMessage();
        SendToAll(MessageBuilder.BuildChatMessage(state.Player, chatMessage.Contents));
    }

    private void SendToOne(ConnectedPlayer player, byte[] data)
    {
        _socket.SendToAsync(data, SocketFlags.None, player.EndPoint);
    }

    private void SendToAll(byte[] data)
    {
        foreach (var client in ConnectedPlayers)
        {
            _socket.SendTo(data, data.Length, SocketFlags.None, client.EndPoint);
        }
    }
}