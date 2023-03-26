namespace Forged.MapServer.Globals;

public struct ExtendedPlayerName
{
    public ExtendedPlayerName(string name, string realmName)
    {
        Name = name;
        Realm = realmName;
    }

    public string Name;
    public string Realm;
}