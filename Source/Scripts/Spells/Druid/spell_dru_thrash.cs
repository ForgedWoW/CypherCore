// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 77758 - Thrash
internal class SpellDruThrash : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHitTarget(int effIndex)
    {
        var hitUnit = HitUnit;

        if (hitUnit != null)
        {
            var caster = Caster;

            caster.SpellFactory.CastSpell(hitUnit, DruidSpellIds.ThrashBearAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }
}