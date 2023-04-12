// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PetLevelInfo
{
    public PetLevelInfo()
    {
        Health = 0;
        Mana = 0;
    }

    public uint Armor { get; set; }
    public uint Health { get; set; }
    public uint Mana { get; set; }
    public uint[] Stats { get; set; } = new uint[(int)Framework.Constants.Stats.Max];
}