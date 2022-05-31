# Zophos

## Notes
### Migrations
To add a DB migration it needs to be done from the Zophos.Data project, with the startup project set as Zophos.Server.

```
cd server\src\Zophos.Data
dotnet ef migrations add <migration name> --verbose --startup-project ..\Zophos.Server\Zophos.Server.csproj
```
