// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

//204488 - Focused Rage
[SpellScript(204488)]
public class spell_warr_focused_rage_prot : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo.Id == WarriorSpells.SHIELD_SLAM;
    }
}

[SpellScript(204488)]
public class spell_warr_focused_rage_prot_SpellScript : SpellScript, IHasSpellEffects
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
            if (caster.HasAura(WarriorSpells.VENGEANCE_AURA))
                caster.CastSpell(caster, WarriorSpells.VENGEANCE_IGNORE_PAIN, true);
    }
}