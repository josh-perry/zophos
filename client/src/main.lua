local batteries = require("lib.batteries")
batteries:export()

local messageBuilders = require("messageBuilders")
local chat = require("chat")

local MessageType = require("schemas.Message")
local UpdatePositionMessage = require("schemas.UpdatePositionMessage")
local PlayerIdMessage = require("schemas.PlayerIdMessage")

local network = require("network")

local player = {}
local otherClients = {}

local function generateUuid4()
    local _random = love.math.random
    local uuidTemplate = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"

    local uuid4 = uuidTemplate:gsub("[xy]", function (c)
        -- x should be 0x0-0xF, the single y should be 0x8-0xB
        return string.format("%x", c == "x" and _random(0x0, 0xF) or _random(0x8, 0xB))
    end)

    return uuid4
end

function addClientIfUnknown(id, x, y, name)
    if otherClients[id] then
        return otherClients[id]
    end

    print("Adding other client!")
    otherClients[id] = {
        x = 0,
        y = 0,
        w = 32,
        h = 32,
        name = name or id or "[Unknown]"
    }

    return otherClients[id]
end

function love.load(args)
    for _, v in ipairs(args) do
        if v:starts_with("name=") then
            player.name = v:split("name=")[2]
            print(player.name)
        end
    end

    player.w = 32
    player.h = 32
    player.x = love.math.random(10, love.graphics.getWidth() - player.w - 10)
    player.y = love.math.random(10, love.graphics.getHeight() - player.h - 10)
    player.speed = 150
    player.id = ""
    player.name = player.name or "Player"

    network:initialize("127.0.0.1", 22122)
    network:send(messageBuilders.connect(player.id))

    network:addCallbackForMessageType(MessageType.ChatMessage, function(baseMessage)
        chat:addMessage(baseMessage)
    end)

    network:addCallbackForMessageType(MessageType.PlayerIdMessage, function(baseMessage)
        local playerIdMessage = PlayerIdMessage.New()
        playerIdMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)

        print("Player id was "..player.id)
        player.id = playerIdMessage:PlayerId()
        print("Updating player id to "..player.id)
    end)

    network:addCallbackForMessageType(MessageType.UpdatePositionMessage, function(baseMessage)
        local updatePositionMessage = UpdatePositionMessage.New()
        updatePositionMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)

        if baseMessage:ClientId() ~= player.id then
            local otherClient = addClientIfUnknown(baseMessage:ClientId())
            otherClient.x = updatePositionMessage:X()
            otherClient.y = updatePositionMessage:Y()
            return
        end

        player.x = updatePositionMessage:X()
        player.y = updatePositionMessage:Y()
    end)

    network:send(messageBuilders.updatePosition(player.id, player.x, player.y))
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
        network:send(messageBuilders.updatePosition(player.id, player.x, player.y))
    end

    network:update(dt)
end

function love.draw()
    love.graphics.setColor(1, 1, 1)
    love.graphics.print(player.name, 10, 10)
    love.graphics.print(player.id, 10, 30)

    if network.falseDisconnect then
        local r = 8
        love.graphics.setColor(1, 0, 0, 1)
        love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)
    else
        local r = 8
        love.graphics.setColor(0, 1, 0, 1 - (love.timer.getTime() - network.heartbeat.lastHeartbeatResponse))
        love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)
    end

    love.graphics.setColor(0.5, 0.5, 1)
    love.graphics.rectangle("fill", math.round(player.x), math.round(player.y), player.w, player.h)

    for i, v in pairs(otherClients) do
        love.graphics.setColor(1, 1, 1)
        love.graphics.rectangle("line", math.round(v.x), math.round(v.y), v.w, v.h)
    end

    chat:draw()
end

function love.keypressed(key)
    if key == "space" then
        network:send(messageBuilders.setName(player.id, "my new name"))
    elseif key == "tab" then
        chat:toggleFocus()
    elseif key == "return" then
        chat:sendMessage(player.id)
    elseif key == "backspace" then
        chat:backspace()
    elseif key == "f2" then
        network.falseDisconnect = not network.falseDisconnect
    end
end

function love.textinput(text)
    chat:appendMessageToSend(text)
end
