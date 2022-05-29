local flatbuffers = require("flatbuffers")

local MessageType = require("schemas.Message")
local ChatMessage = require("schemas.ChatMessage")
local BaseMessage = require("schemas.BaseMessage")
local SetNameMessage = require("schemas.SetNameMessage")
local PlayerConnectMessage = require("schemas.PlayerConnectMessage")
local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local messageBuilders = {}

local defaultBufferSize = 1024

local function buildBaseMessageAndFinish(builder, clientId, message, messageType)
    local buildClientId = builder:CreateString(clientId)

    BaseMessage.Start(builder)

    BaseMessage.AddClientId(builder, buildClientId)
    BaseMessage.AddMessageType(builder, messageType)
    BaseMessage.AddMessage(builder, message)

    local message = BaseMessage.End(builder)
    return builder:Finish(message)
end

function messageBuilders.connect(playerName)
    local builder = flatbuffers.Builder(defaultBufferSize)

    local buildPlayerName = builder:CreateString(playerName)

    PlayerConnectMessage.Start(builder)
    PlayerConnectMessage.AddName(builder, buildPlayerName)
    local connectMessage = PlayerConnectMessage.End(builder)

    buildBaseMessageAndFinish(builder, "", connectMessage, MessageType.PlayerConnectMessage)
    return builder:Output()
end

function messageBuilders.chat(clientId, contents)
    local builder = flatbuffers.Builder(defaultBufferSize)
    local buildClientId = builder:CreateString(clientId)
    local buildContents = builder:CreateString(contents)

    ChatMessage.Start(builder)
    ChatMessage.AddContents(builder, buildContents)
    local chatMessage = ChatMessage.End(builder)

    buildBaseMessageAndFinish(builder, clientId, chatMessage, MessageType.ChatMessage)
    return builder:Output()
end

function messageBuilders.setName(clientId, name)
    local builder = flatbuffers.Builder(defaultBufferSize)
    local buildName = builder:CreateString(name)
    local buildClientId = builder:CreateString(clientId)

    SetNameMessage.Start(builder)
    SetNameMessage.AddName(builder, buildName)
    local setNameMessage = SetNameMessage.End(builder)

    buildBaseMessageAndFinish(builder, clientId, setNameMessage, MessageType.SetNameMessage)
    return builder:Output()
end

function messageBuilders.updatePosition(clientId, x, y)
    local builder = flatbuffers.Builder(defaultBufferSize)

    UpdatePositionMessage.Start(builder)
    UpdatePositionMessage.AddX(builder, x)
    UpdatePositionMessage.AddY(builder, y)
    local updatePositionMessage = UpdatePositionMessage.End(builder)

    buildBaseMessageAndFinish(builder, clientId, updatePositionMessage, MessageType.UpdatePositionMessage)
    return builder:Output()
end

return messageBuilders
