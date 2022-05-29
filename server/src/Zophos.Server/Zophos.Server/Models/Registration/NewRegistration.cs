using Zophos.Data.Models.Db;

namespace Zophos.Server.Models.Registration;

public class NewRegistration
{
    public RegistrationStatus RegistrationStatus { get; set;}

    public Player Player { get; set; }
}