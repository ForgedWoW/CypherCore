// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//190456 - Ignore Pain
[SpellScript(190456)]
public class SpellWarrIgnorePain : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
        {
            if (caster.HasAura(WarriorSpells.RENEWED_FURY))
                caster.SpellFactory.CastSpell(caster, WarriorSpells.RENEWED_FURY_EFFECT, true);

            if (caster.HasAura(WarriorSpells.VENGEANCE_AURA))
                caster.SpellFactory.CastSpell(caster, WarriorSpells.VENGEANCE_FOCUSED_RAGE, true);
        }
    }
}