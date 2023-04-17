// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49020)]
public class SpellDkObliterate : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }


    private void HandleHit(int effIndex)
    {
        Caster.RemoveAura(DeathKnightSpells.KILLING_MACHINE);

        if (Caster.HasAura(DeathKnightSpells.ICECAP))
            if (Caster.SpellHistory.HasCooldown(DeathKnightSpells.PILLAR_OF_FROST))
                Caster.SpellHistory.ModifyCooldown(DeathKnightSpells.PILLAR_OF_FROST, TimeSpan.FromSeconds(-3000));

        if (Caster.HasAura(DeathKnightSpells.INEXORABLE_ASSAULT_STACK))
            Caster.SpellFactory.CastSpell(HitUnit, DeathKnightSpells.INEXORABLE_ASSAULT_DAMAGE, true);

        if (Caster.HasAura(DeathKnightSpells.RIME) && RandomHelper.randChance(45))
            Caster.SpellFactory.CastSpell(null, DeathKnightSpells.RIME_BUFF, true);
    }
}