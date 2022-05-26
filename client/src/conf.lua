local function addAdditionalRequirePaths(paths)
    for _, path in ipairs(paths) do
        love.filesystem.setRequirePath(string.format("%s;%s", love.filesystem.getRequirePath(), path))
    end
end

function love.conf(t)
    addAdditionalRequirePaths({
        "lib/flatbuffers/?.lua"
    })

    t.window.title = "Zophos"
end
