// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 13280 Gnomish Death Ray
internal class SpellItemGnomishDeathRay : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target)
        {
            if (RandomHelper.URand(0, 99) < 15)
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.GNOMISH_DEATH_RAY_SELF, true); // failure
            else
                caster.SpellFactory.CastSpell(target, ItemSpellIds.GNOMISH_DEATH_RAY_TARGET, true);
        }
    }
}