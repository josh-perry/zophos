local flatbuffers = require("flatbuffers")

local testObject = require("schemas.TestObject")
local testFacing = require("schemas.Facing")

local socket = require("socket")
local client

local function buildTestObject()
    local builder = flatbuffers.Builder(1024)
    local message = builder:CreateString("hello world!")

    testObject.Start(builder)
    testObject.AddMessage(builder, message)
    testObject.AddFacing(builder, testFacing.Up)
    local object = testObject.End(builder)

    builder:Finish(object)
    return builder:Output()
end

function love.load()
    client = socket.udp()
    client:settimeout(0)
    client:setpeername("127.0.0.1", 22122)
end

function love.update(dt)
    repeat
        local data = client:receive()

        if data then
            print(string.format("Server says '%s'", data))
        end
    until not data
end

function love.keypressed(key)
    if key == "space" then
        local output = buildTestObject(builder)
        send(output)
    end
end

function send(data)
    print(string.format("Sending server: '%s'", data))
    client:send(data)
end
