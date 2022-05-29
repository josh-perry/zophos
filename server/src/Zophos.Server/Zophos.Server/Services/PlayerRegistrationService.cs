using Zophos.Data.Models.Db;
using Zophos.Data.Repositories.Contracts;
using Zophos.Server.Models.Registration;

namespace Zophos.Server.Services;

public class PlayerRegistrationService : IPlayerRegistrationService
{
    private readonly IPlayerRepository _playerRepository;
    
    public PlayerRegistrationService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }
    
    public NewRegistration RegisterPlayer(Client client, string name)
    {
        var player = new Player
        {
            Name = name
        };
        
        var success = _playerRepository.AddPlayer(player);

        return new NewRegistration
        {
            Player = player,
            RegistrationStatus = success ? RegistrationStatus.Created : RegistrationStatus.Failed
        };
    }
}