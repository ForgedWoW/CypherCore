﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.VISAGE)]
public class SpellEvokerCosmicEvokerVisage : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster.HasAura(EvokerSpells.VISAGE_AURA))
        {
            // Dracthyr Form
            caster.RemoveAura(EvokerSpells.VISAGE_AURA);
            caster.SpellFactory.CastSpell(caster, EvokerSpells.ALTERED_FORM, true);
            caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
            caster.SetDisplayId(108590);
        }
        else
        {
            // Visage Form
            if (caster.HasAura(EvokerSpells.ALTERED_FORM))
                caster.RemoveAura(EvokerSpells.ALTERED_FORM);

            caster.SpellFactory.CastSpell(caster, EvokerSpells.VISAGE_AURA, true);
            caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
            caster.SetDisplayId(104597);
        }
    }
}