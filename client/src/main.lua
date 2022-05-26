local batteries = require("lib.batteries")
batteries:export()

local flatbuffers = require("flatbuffers")

local MessageType = require("schemas.Message")
local BaseMessage = require("schemas.BaseMessage")
local SetNameMessage = require("schemas.SetNameMessage")
local PlayerConnectMessage = require("schemas.PlayerConnectMessage")
local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local socket = require("socket")
local client

local player = {}
local networked_entities = {}

local lastHeartbeat = 0
local heartbeatTimer = 0

local function generateUuid4()
    local _random = love.math.random
    local uuidTemplate = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"

    local uuid4 = uuidTemplate:gsub("[xy]", function (c)
        -- x should be 0x0-0xF, the single y should be 0x8-0xB
        return string.format("%x", c == "x" and _random(0x0, 0xF) or _random(0x8, 0xB))
    end)

    return uuid4
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

local function buildSetNameMessage()
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

local function buildUpdatePositionMessage(x, y)
    local builder = flatbuffers.Builder(1024)

    UpdatePositionMessage.Start(builder)
    UpdatePositionMessage.AddX(builder, x)
    UpdatePositionMessage.AddY(builder, y)
    local updatePositionMessage = UpdatePositionMessage.End(builder)

    buildBaseMessage(builder, updatePositionMessage, MessageType.UpdatePositionMessage)

    return builder:Output()
end

local function updatePlayerPosition(x, y)
    local message = buildUpdatePositionMessage(x, y)
    client:send(message)
end

function love.load()
    client = socket.udp()
    client:settimeout(0)
    client:setpeername("127.0.0.1", 22122)

    player.w = 32
    player.h = 32
    player.x = love.math.random(10, love.graphics.getWidth() - player.w - 10)
    player.y = love.math.random(0, love.graphics.getHeight() - player.h - 10)
    player.speed = 150
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

    local dx, dy = 0, 0

    if love.keyboard.isDown("w") then
        dy = -1
    end
    if love.keyboard.isDown("s") then
        dy = 1
    end
    if love.keyboard.isDown("a") then
        dx = -1
    end
    if love.keyboard.isDown("d") then
        dx = 1
    end

    if dx ~= 0 or dy ~= 0 then
        player.x = player.x + dx * player.speed * dt
        player.y = player.y + dy * player.speed * dt
        updatePlayerPosition(player.x, player.y)
    end

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

    love.graphics.setColor(0.5, 0.5, 1)
    love.graphics.rectangle("fill", math.round(player.x), math.round(player.y), player.w, player.h)

    love.graphics.setColor(1, 1, 1)
    for _, e in ipairs(networked_entities) do
        love.graphics.rectangle("line", e.x, e.y, e.w, e.h)
    end
end

function love.keypressed(key)
    if key == "space" then
        local output = buildSetNameMessage(builder)
        client:send(output)
    end
end
