// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Skills;

internal class SkillPerfectItemEntry
{
    // perfection proc chance
    public double PerfectCreateChance;

    // itemid of the resulting perfect item
    public uint PerfectItemType;

    // the spell id of the spell required - it's named "specialization" to conform with SkillExtraItemEntry
    public uint RequiredSpecialization;
    public SkillPerfectItemEntry(uint rS = 0, double pCC = 0f, uint pIT = 0)
    {
        RequiredSpecialization = rS;
        PerfectCreateChance = pCC;
        PerfectItemType = pIT;
    }
}