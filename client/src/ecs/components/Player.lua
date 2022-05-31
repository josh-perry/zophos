return Concord.component("player", function(c, name, localPlayer)
    c.name = name or "DefaultName"
    c.localPlayer = localPlayer or false
end)
