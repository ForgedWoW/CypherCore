// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 48681 - Summon Silverbrook Worgen
internal class SpellQ12308EscapeFromSilverbrookSummonWorgen : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new DestinationTargetSelectHandler(ModDest, 0, Targets.DestCasterSummon));
    }

    private void ModDest(SpellDestination dest)
    {
        var dist = GetEffectInfo(0).CalcRadius(Caster);
        var angle = RandomHelper.FRand(0.75f, 1.25f) * MathFunctions.PI;

        var pos = Caster.GetNearPosition(dist, angle);
        dest.Relocate(pos);
    }
}