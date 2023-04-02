// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PetLevelInfo
{
    public uint Armor;
    public uint Health;
    public uint Mana;
    public uint[] Stats = new uint[(int)Framework.Constants.Stats.Max];
    public PetLevelInfo()
    {
        Health = 0;
        Mana = 0;
    }
}