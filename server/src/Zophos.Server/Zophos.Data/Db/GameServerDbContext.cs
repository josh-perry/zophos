using Zophos.Data.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace Zophos.Data.Db;

public class GameServerDbContext : DbContext
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options)
        : base(options)
    {
        Database.Migrate();
    }

    public DbSet<Player> Players { get; set; }
}