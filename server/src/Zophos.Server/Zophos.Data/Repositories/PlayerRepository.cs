using Zophos.Data.Db;
using Zophos.Data.Models.Db;
using Zophos.Data.Repositories.Contracts;

namespace Zophos.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly GameServerDbContext _gameServerDbContext;

    public PlayerRepository(GameServerDbContext gameServerDbContext)
    {
        _gameServerDbContext = gameServerDbContext;
    }
    
    public Player? GetPlayerById(string id)
    {
        return _gameServerDbContext.Players.FirstOrDefault(x => x.Id.ToString() == id);
    }

    public bool AddPlayer(Player player)
    {
        _gameServerDbContext.Add(player);
        return _gameServerDbContext.SaveChanges() != 0;
    }
}