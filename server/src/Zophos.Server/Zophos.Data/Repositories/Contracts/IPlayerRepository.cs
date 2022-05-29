using Zophos.Data.Models.Db;

namespace Zophos.Data.Repositories.Contracts;

public interface IPlayerRepository
{
    Player? GetPlayerById(string id);

    bool AddPlayer(Player player);
}