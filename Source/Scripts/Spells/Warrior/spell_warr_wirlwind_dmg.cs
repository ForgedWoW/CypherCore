// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 1680 Whirlwind
[SpellScript(1680)]
public class SpellWarrWirlwindDmg : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleOnHitTarget(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (caster != null)
            if (caster.HasAura(202316)) // Fervor of Battle
            {
                var target = caster.SelectedUnit;

                if (target != null)
                    if (caster.IsValidAttackTarget(target))
                        caster.SpellFactory.CastSpell(target, WarriorSpells.SLAM_ARMS, true);
            }
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
    }
}