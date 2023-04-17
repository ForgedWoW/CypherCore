// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(80313)]
public class SpellDruidPulverize : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHitTarget, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHitTarget(int effIndex)
    {
        var target = HitUnit;

        if (target != null)
        {
            target.RemoveAura(Spells.TrashDotTwoStacksMarker);
            Caster.SpellFactory.CastSpell(target, Spells.PulverizeDamageReductionBuff, true);
        }
    }

    private struct Spells
    {
        public static readonly uint Pulverize = 80313;
        public static readonly uint TrashDotTwoStacksMarker = 158790;
        public static readonly uint PulverizeDamageReductionBuff = 158792;
    }
}