// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 205290 - Wake of Ashes
[SpellScript(205290)]
public class SpellPalWakeOfAshes : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamages, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamages(int effIndex)
    {
        var target = HitCreature;

        if (target != null)
        {
            var creTemplate = target.Template;

            if (creTemplate != null)
                if (creTemplate.CreatureType == CreatureType.Demon || creTemplate.CreatureType == CreatureType.Undead)
                    Caster.SpellFactory.CastSpell(target, PaladinSpells.WAKE_OF_ASHES_STUN, true);
        }
    }
}