// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[SpellScript(23881)] // 23881 - Bloodthirst
internal class SpellWarrBloodthirst : SpellScript, IHasSpellEffects, ISpellOnCast, ISpellOnHit
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 3, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    public void OnCast()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        if (target != ObjectAccessor.Instance.GetUnit(caster, caster.Target))
            HitDamage = HitDamage / 2;

        if (caster.HasAura(WarriorSpells.FRESH_MEAT))
            if (RandomHelper.FRand(0, 15) != 0)
                caster.SpellFactory.CastSpell(null, WarriorSpells.ENRAGE_AURA, true);

        if (caster.HasAura(WarriorSpells.THIRST_FOR_BATTLE))
            caster.AddAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF, caster);
    }

    public void OnHit()
    {
        Caster.SpellFactory.CastSpell(Caster, WarriorSpells.BLOODTHIRST_HEAL, true);
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, WarriorSpells.BLOODTHIRST_HEAL, true);
    }
}