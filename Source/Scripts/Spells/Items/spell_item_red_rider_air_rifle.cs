// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemRedRiderAirRifle : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var caster = Caster;
        var target = HitUnit;

        if (target)
        {
            caster.SpellFactory.CastSpell(caster, ItemSpellIds.AIR_RIFLE_HOLD_VISUAL, true);
            // needed because this spell shares GCD with its triggered spells (which must not be cast with triggered flag)
            var player = caster.AsPlayer;

            if (player)
                player.SpellHistory.CancelGlobalCooldown(SpellInfo);

            if (RandomHelper.URand(0, 4) != 0)
                caster.SpellFactory.CastSpell(target, ItemSpellIds.AIR_RIFLE_SHOOT, false);
            else
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.AIR_RIFLE_SHOOT_SELF, false);
        }
    }
}