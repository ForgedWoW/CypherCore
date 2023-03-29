// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

public class SpellValue
{
    public Dictionary<int, double> EffectBasePoints = new();
    public uint CustomBasePointsMask;
    public uint MaxAffectedTargets;
    public float RadiusMod;
    public int AuraStackAmount;
    public float DurationMul;
    public float CriticalChance;
    public int? Duration;
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