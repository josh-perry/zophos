using Zophos.Data;
using Zophos.Server.Services;

namespace Zophos.Server;

public interface IMessageNotifier
{
    void MessageReceived(MessageState state);

    void AddHandler(Message messageType, MessageNotifier.MessageEventHandler handler);
}