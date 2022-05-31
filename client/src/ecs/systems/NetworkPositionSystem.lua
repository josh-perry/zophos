local network = require("network")
local messageBuilders = require("messageBuilders")

local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local NetworkPositionSystem = Concord.system({
    pool = { "position", "moveable", "networked" }
})

function NetworkPositionSystem:UpdatePositionMessageReceived(baseMessage, updatePositionMessage)
    for _, e in ipairs(self.pool) do
        if baseMessage:ClientId() == e.networked.id then
            e.position.x = updatePositionMessage:X()
            e.position.y = updatePositionMessage:Y()
            return
        end
    end

    if #self.pool > 0 then
        network.unknownIds[baseMessage:ClientId()] = true
    end
end

return NetworkPositionSystem
