// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

public class SpellValue
{
    public int AuraStackAmount;
    public float CriticalChance;
    public uint CustomBasePointsMask;
    public int? Duration;
    public float DurationMul;
    public Dictionary<int, double> EffectBasePoints = new();
    public uint MaxAffectedTargets;
    public float RadiusMod;
    public double? SummonDuration;

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
}