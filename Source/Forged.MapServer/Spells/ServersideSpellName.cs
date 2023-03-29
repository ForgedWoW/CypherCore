// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.S;
using Framework.Constants;

namespace Forged.MapServer.Spells;

internal struct ServersideSpellName
{
    public SpellNameRecord Name;

    public ServersideSpellName(uint id, string name)
    {
        Name = new SpellNameRecord
        {
            Name = new LocalizedString(),
            Id = id
        };

        for (Locale i = 0; i < Locale.Total; ++i)
            Name.Name[i] = name;
    }
}