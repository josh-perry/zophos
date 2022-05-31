local batteries = require("lib.batteries")
batteries:export()

local messageBuilders = require("messageBuilders")
local chat = require("chat")

local MessageType = require("schemas.Message")
local UpdatePositionMessage = require("schemas.UpdatePositionMessage")
local PlayerIdMessage = require("schemas.PlayerIdMessage")
local PlayerInitMessage = require("schemas.PlayerInitMessage")

local network = require("network")

local player = {}
local otherClients = {}

_G.Concord = require("lib.concord")

local Systems = {}
Concord.utils.loadNamespace("ecs/components")
Concord.utils.loadNamespace("ecs/systems", Systems)

local world = Concord.world()

function love.load(args)
    local playerName = "DefaultName"

    for _, v in ipairs(args) do
        if v:starts_with("name=") then
            playerName = v:split("name=")[2]
        end
    end

    for _, system in pairs(Systems) do
        world:addSystems(system)
    end

    local w, h = 32, 32
    local x = love.math.random(10, love.graphics.getWidth() - w - 10)
    local y = love.math.random(10, love.graphics.getHeight() - h - 10)

    local playerEntity = Concord.entity(world)
        :give("drawable")
        :give("moveable")
        :give("player", playerName, true)
        :give("position", x, y)
        :give("moveable", 150)
        :give("networked", "")
        :give("controlled")

    network:initialize("127.0.0.1", 22122)
    network:send(messageBuilders.connect(playerName))

    network:addCallbackForMessageType(MessageType.ChatMessage, function(baseMessage)
        chat:addMessage(baseMessage)
    end)

    network:addCallbackForMessageType(MessageType.PlayerIdMessage, function(baseMessage)
        local playerIdMessage = PlayerIdMessage.New()
        playerIdMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)
        network.id = playerIdMessage:PlayerId()
        world:emit("PlayerIdMessageReceived", baseMessage, playerIdMessage)
    end)

    network:addCallbackForMessageType(MessageType.UpdatePositionMessage, function(baseMessage)
        local updatePositionMessage = UpdatePositionMessage.New()
        updatePositionMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)
        world:emit("UpdatePositionMessageReceived", baseMessage, updatePositionMessage)
    end)

    network:addCallbackForMessageType(MessageType.PlayerInitMessage, function(baseMessage)
        local playerInitMessage = PlayerInitMessage.New()
        playerInitMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)

        print("new player!")

        -- Add player
        local otherPlayer = Concord.entity(world)
            :give("drawable")
            :give("moveable")
            :give("player", playerInitMessage:Name(), false)
            :give("position", playerInitMessage:X(), playerInitMessage:Y())
            :give("moveable", 150)
            :give("networked", playerInitMessage:Id())

        world:emit("PlayerInitMessageReceived", baseMessage, playerInitMessage)
    end)
end

function love.update(dt)
    network:update(dt)
    world:emit("update", dt)
end

function love.draw()
    if network.falseDisconnect then
        local r = 8
        love.graphics.setColor(1, 0, 0, 1)
        love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)
    else
        local r = 8
        love.graphics.setColor(0, 1, 0, 1 - (love.timer.getTime() - network.heartbeat.lastHeartbeatResponse))
        love.graphics.circle("fill", love.graphics.getWidth() - 10 - r * 2, 10 + r * 2, r)
    end

    for i, v in pairs(otherClients) do
        love.graphics.setColor(1, 1, 1)
        love.graphics.rectangle("line", math.round(v.x), math.round(v.y), v.w, v.h)
    end

    chat:draw()
    world:emit("draw")
end

function love.keypressed(key)
    if key == "tab" then
        chat:toggleFocus()
    elseif key == "return" then
        chat:sendMessage(network.id)
    elseif key == "backspace" then
        chat:backspace()
    elseif key == "f2" then
        network.falseDisconnect = not network.falseDisconnect
    end
end

function love.textinput(text)
    chat:appendMessageToSend(text)
end
