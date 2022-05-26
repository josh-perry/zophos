﻿namespace Zophos.Server;

public class Client
{
    public readonly string ClientId;

    public string Name { get; set; } = string.Empty;

    public float X { get; set; } = 0f;
    
    public float Y { get; set; } = 0f;

    public Client(string clientId)
    {
        ClientId = clientId;
    }
}