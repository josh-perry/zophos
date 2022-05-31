using System.Net;
using Zophos.Data;
using Zophos.Data.Models.Db;

namespace Zophos.Server.Models;

public class MessageState
{
    public BaseMessage BaseMessage;

    public EndPoint Remote;

    public Player? Player;
}