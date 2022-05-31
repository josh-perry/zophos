using Microsoft.Extensions.Logging;
using Zophos.Data;
using Zophos.Server.Models;

namespace Zophos.Server.Services;

public class MessageNotifier : IMessageNotifier
{
    public delegate void MessageEventHandler(MessageState messageState);

    private readonly Dictionary<Message, MessageEventHandler> _messageEventHandlers = new Dictionary<Message, MessageEventHandler>();
    
    private readonly ILogger<MessageNotifier> _logger;
    
    public MessageNotifier(ILogger<MessageNotifier> logger)
    {
        _logger = logger;
    }

    public void MessageReceived(MessageState state)
    {
        _messageEventHandlers[state.BaseMessage.MessageType].Invoke(state);
    }

    public void AddHandler(Message type, MessageEventHandler handler)
    {
        _logger.LogInformation("New event handler for message type '{}'", new[] { type });
        
        if (!_messageEventHandlers.ContainsKey(type))
        {
            _messageEventHandlers[type] = handler;
            return;
        }
        
        _messageEventHandlers[type] += handler;
    }
}