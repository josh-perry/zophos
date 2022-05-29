using System.Net;
using Zophos.Data.Models.Db;

namespace Zophos.Server.Models;

public class ConnectedPlayer
{
    public Player? Player;

    public EndPoint EndPoint;
}