local ChatMessage = require("schemas.ChatMessage")

local messageBuilders = require("messageBuilders")
local network = require("network")

local chat = {}

chat.messages = {}
chat.messageToSend = ""

function chat:addMessage(baseMessage)
    local chatMessage = ChatMessage.New()
    chatMessage:Init(baseMessage:Message().bytes, baseMessage:Message().pos)

    table.insert(self.messages, {
        speaker = baseMessage:ClientId(),
        message = chatMessage:Contents()
    })
end

function chat:draw()
    local lineHeight = 24
    local showLastMessagesCount = 5

    love.graphics.setColor(1, 1, 1)

    for i = showLastMessagesCount, 1, -1 do
        local m = self.messages[#self.messages-showLastMessagesCount+i]

        if m then
            local y = love.graphics.getHeight() - (showLastMessagesCount - i + 2) * lineHeight
            love.graphics.print(string.format("%s:\t%s", m.speaker, m.message), 10, y)
        end
    end
    love.graphics.print("> "..self.messageToSend, 10, love.graphics.getHeight() - lineHeight)
end

function chat:update()
end

function chat:sendMessage(clientId)
    if #self.messageToSend <= 0 then
        return
    end

    network:send(messageBuilders.chat(clientId, self.messageToSend))
    self.messageToSend = ""
end

function chat:setMessageToSend(messageToSend)
    self.messageToSend = messageToSend
end

function chat:appendMessageToSend(toAppend)
    self.messageToSend = self.messageToSend..toAppend
end

function chat:backspace()
    local byteOffset = utf8.offset(self.messageToSend, -1)

    if byteOffset then
        self.messageToSend = string.sub(self.messageToSend, 1, byteOffset - 1)
    end
end

return chat
