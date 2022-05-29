using Zophos.Server.Models.Registration;

namespace Zophos.Server;

public interface IPlayerRegistrationService
{
    NewRegistration RegisterPlayer(Client client, string name);
}