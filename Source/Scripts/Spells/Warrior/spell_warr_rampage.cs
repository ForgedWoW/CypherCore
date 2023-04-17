﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Rampage damage dealers - 218617, 184707, 184709, 201364, 201363
[SpellScript(new uint[]
{
    218617, 184707, 184709, 201364, 201363
})]
public class SpellWarrRampage : SpellScript, ISpellOnHit, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var enrage = caster.GetAura(WarriorSpells.ENRAGE_AURA);

        if (enrage != null)
            enrage.RefreshDuration();
        else
            caster.SpellFactory.CastSpell(caster, WarriorSpells.ENRAGE_AURA, true);
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        if (target != ObjectAccessor.Instance.GetUnit(caster, caster.Target))
            HitDamage = HitDamage / 2;

        if (caster == target)
            HitDamage = 0;
    }
}