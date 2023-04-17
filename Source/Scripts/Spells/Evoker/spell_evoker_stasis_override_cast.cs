// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_OVERRIDE_SPELL)]
public class SpellEvokerStasisOverrideCast : SpellScript, ISpellOnCast
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
            if (a.ScriptValues.TryGetValue("spell", out var obj))
            {
                if (obj == null) return;

                var spell = (Spell)obj;

                player.SpellFactory.CastSpell(spell.Targets,
                                              spell.SpellInfo.Id,
                                              new CastSpellExtraArgs(true)
                                              {
                                                  EmpowerStage = spell.EmpoweredStage
                                              });
            }
        });

        orbAura.Remove();
    }
}