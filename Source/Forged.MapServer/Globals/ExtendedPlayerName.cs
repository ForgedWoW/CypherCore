// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public struct ExtendedPlayerName
{
    public string Name;

    public string Realm;

    public ExtendedPlayerName(string name, string realmName)
    {
        Name = name;
        Realm = realmName;
    }
}