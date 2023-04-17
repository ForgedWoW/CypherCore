// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Skills;

internal class SkillPerfectItemEntry
{
    public SkillPerfectItemEntry(uint requiredSpecialization = 0, double perfectCreateChance = 0f, uint perfectItemType = 0)
    {
        RequiredSpecialization = requiredSpecialization;
        PerfectCreateChance = perfectCreateChance;
        PerfectItemType = perfectItemType;
    }

    // perfection proc chance
    public double PerfectCreateChance { get; set; }

    // itemid of the resulting perfect item
    public uint PerfectItemType { get; set; }

    // the spell id of the spell required - it's named "specialization" to conform with SkillExtraItemEntry
    public uint RequiredSpecialization { get; set; }
}