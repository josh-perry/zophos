local socket = require("socket")
local flatbuffers = require("flatbuffers")
local messageBuilders = require("messageBuilders")

local BaseMessage = require("schemas.BaseMessage")

local network = {}

network.client = {}
network.queuedMessages = {}
network.callbacks = {}
network.id = nil
network.unknownIds = {}
network.falseDisconnect = false

network.heartbeat = {
    lastHeartbeatResponse = 0,
    nextHeartbeatSeconds = 0,
    update = function(self, dt)
        self.nextHeartbeatSeconds = self.nextHeartbeatSeconds - dt

        if self.nextHeartbeatSeconds <= 0 then
            self.nextHeartbeatSeconds = 1
        end
    end
}

function network:initialize(address, port)
    self.client = socket.udp()
    self.client:settimeout(0)
    self.client:setpeername(address, port)
end

function network:handleMessage(baseMessage)
    local messageType = baseMessage:MessageType()

    if not self.callbacks[messageType] then
        return
    end

    for _, c in ipairs(self.callbacks[messageType]) do
        c(baseMessage)
    end
end

function network:update(dt)
    if self.falseDisconnect then
        return
    end

    repeat
        local data = self.client:receive()

        if data then
            self.heartbeat.lastHeartbeatResponse = love.timer.getTime()

            local buffer = flatbuffers.binaryArray.New(data)
            local message = BaseMessage.GetRootAsBaseMessage(buffer, 0)
            self:handleMessage(message)
        end
    until not data

    for id, _ in pairs(self.unknownIds) do
        print(string.format("Requesting info for %s", id))
        self:send(messageBuilders.RequestEntityInitInfoMessage(self.id, id))
        self.unknownIds[id] = nil
    end

    self.heartbeat:update(dt)
    self:flushMessages()
end

function network:flushMessages()
    if self.falseDisconnect then
        return
    end

    for i = #self.queuedMessages, 1, -1 do
        local message = self.queuedMessages[i]

        self.client:send(message)
        table.remove(self.queuedMessages, i)
    end
end

function network:send(message)
    table.insert(self.queuedMessages, message)
end

function network:addCallbackForMessageType(messageType, func)
    if not self.callbacks[messageType] then
        self.callbacks[messageType] = {}
    end

    table.insert(self.callbacks[messageType], func)
end

return network
