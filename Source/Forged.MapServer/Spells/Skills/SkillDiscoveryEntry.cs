// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Skills;

public class SkillDiscoveryEntry
{
    public uint SpellId;       // discavered spell
    public uint ReqSkillValue; // skill level limitation
    public double Chance;      // chance

    public SkillDiscoveryEntry(uint spellId = 0, uint reqSkillVal = 0, double chance = 0)
    {
        SpellId = spellId;
        ReqSkillValue = reqSkillVal;
        Chance = chance;
    }
}