// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

public class SpellValue
{
    public SpellValue(SpellInfo proto, WorldObject caster)
    {
        foreach (var spellEffectInfo in proto.Effects)
            EffectBasePoints[spellEffectInfo.EffectIndex] = spellEffectInfo.CalcBaseValue(caster, null, 0, -1);

        CustomBasePointsMask = 0;
        MaxAffectedTargets = proto.MaxAffectedTargets;
        RadiusMod = 1.0f;
        AuraStackAmount = 1;
        CriticalChance = 0.0f;
        DurationMul = 1;
    }

    public int AuraStackAmount { get; set; }
    public float CriticalChance { get; set; }
    public uint CustomBasePointsMask { get; set; }
    public int? Duration { get; set; }
    public float DurationMul { get; set; }
    public Dictionary<int, double> EffectBasePoints { get; set; } = new();
    public uint MaxAffectedTargets { get; set; }
    public float RadiusMod { get; set; }
    public double? SummonDuration { get; set; }
}