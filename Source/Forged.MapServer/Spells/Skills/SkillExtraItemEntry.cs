// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Skills;

internal class SkillExtraItemEntry
{
    public SkillExtraItemEntry(uint requiredSpecialization = 0, double additionalCreateChance = 0f, byte additionalMaxNum = 0)
    {
        RequiredSpecialization = requiredSpecialization;
        AdditionalCreateChance = additionalCreateChance;
        AdditionalMaxNum = additionalMaxNum;
    }

    // the chance to create one additional item
    public double AdditionalCreateChance { get; set; }

    // maximum number of extra items created per crafting
    public byte AdditionalMaxNum { get; set; }

    // the spell id of the specialization required to create extra items
    public uint RequiredSpecialization { get; set; }
}