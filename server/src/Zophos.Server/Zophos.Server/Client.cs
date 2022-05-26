namespace Zophos.Server;

public class Client
{
    public readonly string ClientId;

    public string Name { get; set; } = string.Empty;

    public Client(string clientId)
    {
        ClientId = clientId;
    }
}