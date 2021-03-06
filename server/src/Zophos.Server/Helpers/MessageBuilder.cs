using FlatBuffers;
using Zophos.Data;
using Zophos.Data.Models.Db;

namespace Zophos.Server.Helpers;

public static class MessageBuilder
{
    
    private static void BuildBaseMessageAndFinish(FlatBufferBuilder builder, Player player, int messageOffset, Message messageType)
    {
        var buildClientId = builder.CreateString(player.Id.ToString());
        BaseMessage.StartBaseMessage(builder);
        BaseMessage.AddMessageType(builder, messageType);
        BaseMessage.AddMessage(builder, messageOffset);
        BaseMessage.AddClientId(builder, buildClientId);
        
        var baseMessage = BaseMessage.EndBaseMessage(builder);
        builder.Finish(baseMessage.Value);
    }

    public static byte[] BuildChatMessage(Player speaker, string contents)
    {
        var builder = new FlatBufferBuilder(1024);
        
        var buildContents = builder.CreateString(contents);
        var buildSourceClientId = builder.CreateString(speaker.Id.ToString());
        var buildDestinationSourceId = builder.CreateString(string.Empty);
        ChatMessage.StartChatMessage(builder);
        ChatMessage.AddContents(builder, buildContents);
        ChatMessage.AddSourceClientId(builder, buildSourceClientId);
        ChatMessage.AddDestinationClientId(builder, buildDestinationSourceId);
        var buildChatMessage = ChatMessage.EndChatMessage(builder);
        
        BuildBaseMessageAndFinish(builder, speaker, buildChatMessage.Value, Message.ChatMessage);
        return builder.SizedByteArray();
    }

    public static byte[] BuildUpdatePositionMessage(Player subjectPlayer)
    {
        var builder = new FlatBufferBuilder(1024);
        
        UpdatePositionMessage.StartUpdatePositionMessage(builder);
        UpdatePositionMessage.AddX(builder, subjectPlayer.X);
        UpdatePositionMessage.AddY(builder, subjectPlayer.Y);
        var buildUpdatePositionMessage = UpdatePositionMessage.EndUpdatePositionMessage(builder);
        
        BuildBaseMessageAndFinish(builder, subjectPlayer, buildUpdatePositionMessage.Value, Message.UpdatePositionMessage);
        return builder.SizedByteArray();
    }

    public static byte[] BuildPlayerIdMessage(Player player)
    {
        var builder = new FlatBufferBuilder(1024);

        var buildPlayerId = builder.CreateString(player.Id.ToString());
        PlayerIdMessage.StartPlayerIdMessage(builder);
        PlayerIdMessage.AddPlayerId(builder, buildPlayerId);
        var buildPlayerIdMessage = PlayerIdMessage.EndPlayerIdMessage(builder);
        
        BuildBaseMessageAndFinish(builder, player, buildPlayerIdMessage.Value, Message.PlayerIdMessage);
        return builder.SizedByteArray();
    }
    
    public static byte[] BuildPlayerInitMessage(Player player)
    {
        var builder = new FlatBufferBuilder(1024);
        
        var buildPlayerId = builder.CreateString(player.Id.ToString());
        var buildPlayerName = builder.CreateString(player.Name);
        
        PlayerInitMessage.StartPlayerInitMessage(builder);
        PlayerInitMessage.AddId(builder, buildPlayerId);
        PlayerInitMessage.AddName(builder, buildPlayerName);
        PlayerInitMessage.AddX(builder, player.X);
        PlayerInitMessage.AddY(builder, player.Y);
        var buildPlayerIdMessage = PlayerInitMessage.EndPlayerInitMessage(builder);
        
        BuildBaseMessageAndFinish(builder, player, buildPlayerIdMessage.Value, Message.PlayerInitMessage);
        return builder.SizedByteArray();
    }
}