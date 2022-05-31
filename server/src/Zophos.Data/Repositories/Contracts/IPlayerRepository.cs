using Zophos.Data.Models.Db;

namespace Zophos.Data.Repositories.Contracts;

public interface IPlayerRepository
{
    Player? GetPlayerById(string id);

    Player? GetPlayerByName(string name);

    bool AddPlayer(Player player);
    
    void Save(IEnumerable<Player> connectedPlayers);
}