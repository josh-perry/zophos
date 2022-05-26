local flatbuffers = require("flatbuffers")

local MessageType = require("schemas.Message")
local BaseMessage = require("schemas.BaseMessage")
local SetNameMessage = require("schemas.SetNameMessage")
local PlayerConnectMessage = require("schemas.PlayerConnectMessage")

local socket = require("socket")
local client

local player = {}

local lastHeartbeat = 0
local heartbeatTimer = 0

local function generateUuid4()
    local template = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"

    return string.gsub(template, "[xy]", function (c)
        local v = (c == "x") and love.math.random(0, 0xf) or love.math.random(8, 0xb)
        return string.format("%x", v)
    end)
end

local function buildBaseMessage(builder, message, messageType)
    local clientId = builder:CreateString(player.clientId)

    BaseMessage.Start(builder)

    BaseMessage.AddClientId(builder, clientId)
    BaseMessage.AddMessageType(builder, messageType)
    BaseMessage.AddMessage(builder, message)

    local message = BaseMessage.End(builder)
    return builder:Finish(message)
end

local function buildConnectMessage()
    local builder = flatbuffers.Builder(1024)

    PlayerConnectMessage.Start(builder)
    local connectMessage = PlayerConnectMessage.End(builder)

    buildBaseMessage(builder, connectMessage, MessageType.PlayerConnectMessage)

    return builder:Output()
end

local function buildTestObject()
    local builder = flatbuffers.Builder(1024)
    local name = builder:CreateString(player.name)
    local clientId = builder:CreateString(player.clientId)

    SetNameMessage.Start(builder)
    SetNameMessage.AddName(builder, name)
    local setNameMessage = SetNameMessage.End(builder)

    BaseMessage.Start(builder)
    BaseMessage.AddMessageType(builder, MessageType.SetNameMessage)
    BaseMessage.AddMessage(builder, setNameMessage)
    BaseMessage.AddClientId(builder, clientId)
    local object = BaseMessage.End(builder)

    builder:Finish(object)
    return builder:Output()
end

function love.load()
    client = socket.udp()
    client:settimeout(0)
    client:setpeername("127.0.0.1", 22122)

    player.clientId = generateUuid4()
    player.name = string.format("Player (%s)", player.clientId)

    client:send(buildConnectMessage())
end

function love.update(dt)
    repeat
        local data = client:receive()

        if data then
            lastHeartbeat = love.timer.getTime()
        end
    until not data

    heartbeatTimer = heartbeatTimer - dt
    if heartbeatTimer <= 0 then
        heartbeatTimer = 1
        client:send("")
    end
end

function love.draw()
    love.graphics.setColor(1, 1, 1)
    love.graphics.print(player.name, 10, 10)
    love.graphics.print(player.clientId, 10, 30)

    local r = 8
    love.graphics.setColor(0, 1, 0, 1 - (love.timer.getTime() - lastHeartbeat))
    love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)
end

function love.keypressed(key)
    if key == "space" then
        local output = buildTestObject(builder)
        send(output)
    end
end

function send(data)
    client:send(data)
end
