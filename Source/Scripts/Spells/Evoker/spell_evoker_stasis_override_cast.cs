﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_OVERRIDE_SPELL)]
public class spell_evoker_stasis_override_cast : SpellScript, ISpellOnCast
{
	public void OnCast()
    {
        if (!Caster.TryGetAsPlayer(out var player))
            return;

        if (player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_1, out var orbAura))
            CastSpell(player, orbAura);

        if (player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_2, out orbAura))
            CastSpell(player, orbAura);

        if (player.TryGetAura(EvokerSpells.STASIS_ORB_AURA_3, out orbAura))
            CastSpell(player, orbAura);

        player.RemoveAura(EvokerSpells.STASIS_OVERRIDE_AURA);
    }



    void CastSpell(Player player, Aura orbAura)
    {
        if (orbAura == null) return;

        orbAura.ForEachAuraScript<IAuraScriptValues>(a =>
        {
            if (a.ScriptValues.TryGetValue("spell", out object obj))
            {
                if (obj == null) return;

                Spell spell = (Spell)obj;
                player.CastSpell(spell.Targets, spell.SpellInfo.Id, new CastSpellExtraArgs(true) { EmpowerStage = spell.EmpoweredStage });
            }
        });

        orbAura.Remove();
    }
}