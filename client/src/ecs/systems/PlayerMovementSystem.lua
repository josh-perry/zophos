local network = require("network")
local messageBuilders = require("messageBuilders")

local UpdatePositionMessage = require("schemas.UpdatePositionMessage")

local PlayerMovementSystem = Concord.system({
    pool = { "player", "position", "moveable", "networked", "controlled" }
})

function PlayerMovementSystem:update(dt)
    for _, e in ipairs(self.pool) do
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
            e.position.x = e.position.x + dx * e.moveable.speed * dt
            e.position.y = e.position.y + dy * e.moveable.speed * dt

            network:send(messageBuilders.updatePosition(e.networked.id, e.position.x, e.position.y))
        end
    end
end

return PlayerMovementSystem
