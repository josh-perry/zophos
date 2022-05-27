local utf8 = require("utf8")

local batteries = require("lib.batteries")
batteries:export()

local flatbuffers = require("flatbuffers")
local messageBuilders = require("messageBuilders")
local chat = require("chat")

local MessageType = require("schemas.Message")
local BaseMessage = require("schemas.BaseMessage")
local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local network = require("network")

local socket = require("socket")

local player = {}

local function generateUuid4()
    local _random = love.math.random
    local uuidTemplate = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"

    local uuid4 = uuidTemplate:gsub("[xy]", function (c)
        -- x should be 0x0-0xF, the single y should be 0x8-0xB
        return string.format("%x", c == "x" and _random(0x0, 0xF) or _random(0x8, 0xB))
    end)

    return uuid4
end

function love.load()
    player.w = 32
    player.h = 32
    player.x = love.math.random(10, love.graphics.getWidth() - player.w - 10)
    player.y = love.math.random(0, love.graphics.getHeight() - player.h - 10)
    player.speed = 150
    player.clientId = generateUuid4()
    player.name = string.format("Player (%s)", player.clientId)

    network:initialize("127.0.0.1", 22122)
    network:send(messageBuilders.connect(player.clientId))

    network:addCallbackForMessageType(MessageType.ChatMessage, function(baseMessage)
        chat:addMessage(baseMessage)
    end)
end

function love.update(dt)
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
        network:send(messageBuilders.updatePosition(player.clientId, player.x, player.y))
    end

    network:update(dt)
end

function love.draw()
    love.graphics.setColor(1, 1, 1)
    love.graphics.print(player.name, 10, 10)
    love.graphics.print(player.clientId, 10, 30)

    local r = 8
    love.graphics.setColor(0, 1, 0, 1 - (love.timer.getTime() - network.heartbeat.lastHeartbeatResponse))
    love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)

    love.graphics.setColor(0.5, 0.5, 1)
    love.graphics.rectangle("fill", math.round(player.x), math.round(player.y), player.w, player.h)

    chat:draw()
end

function love.keypressed(key)
    if key == "space" then
        network:send(messageBuilders.setName(player.clientId, "my new name"))
    elseif key == "return" then
        chat:sendMessage(player.clientId)
    elseif key == "backspace" then
        chat:backspace()
    end
end

function love.textinput(text)
    chat:appendMessageToSend(text)
end
