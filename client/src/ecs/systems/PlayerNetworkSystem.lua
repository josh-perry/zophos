local network = require("network")
local messageBuilders = require("messageBuilders")

local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local PlayerNetworkSystem = Concord.system({
    pool = { "player", "networked" }
})

function PlayerNetworkSystem:PlayerIdMessageReceived(baseMessage, playerIdMessage)
    for _, e in ipairs(self.pool) do
        e.networked.id = baseMessage:ClientId()
    end
end

return PlayerNetworkSystem
