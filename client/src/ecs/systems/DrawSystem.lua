local DrawSystem = Concord.system({
    pool = { "drawable", "position" },
    playerPool = { "player", "networked" }
})

function DrawSystem:drawDebug()
    for _, e in ipairs(self.playerPool) do
        if e.player.localPlayer then
            love.graphics.setColor(1, 1, 1)
            love.graphics.print(e.player.name, 10, 10)
            love.graphics.print(e.networked.id, 10, 30)

            return
        end
    end
end

function DrawSystem:draw()
    love.graphics.setColor(1, 0, 0)

    for _, e in ipairs(self.pool) do
        love.graphics.rectangle("fill", e.position.x, e.position.y, 32, 32)
    end

    self:drawDebug()
    love.graphics.setColor(1, 1, 1)
end

return DrawSystem
