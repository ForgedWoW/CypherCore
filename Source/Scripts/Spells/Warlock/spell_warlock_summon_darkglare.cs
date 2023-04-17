// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Summon Darkglare - 205180
[SpellScript(205180)]
public class SpellWarlockSummonDarkglare : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHitTarget(int effIndex)
    {
        var target = HitUnit;

        if (target != null)
        {
            var effectList = target.GetAuraEffectsByType(AuraType.PeriodicDamage);

            foreach (var effect in effectList)
            {
                var aura = effect.Base;

                if (aura != null)
                    aura.ModDuration(8 * Time.IN_MILLISECONDS);
            }
        }
    }
}