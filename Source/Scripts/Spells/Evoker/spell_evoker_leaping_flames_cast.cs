// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME)]
internal class SpellEvokerLeapingFlamesCast : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        if (Caster.TryGetAura(EvokerSpells.LEAPING_FLAMES_AURA, out var aura))
        {
            var caster = Caster;
            var spellTarget = ExplTargetUnit;

            // get targets
            var targetList = new List<Unit>();
            uint spell = 0;

            if (caster.IsFriendlyTo(spellTarget))
            {
                spell = EvokerSpells.RED_LIVING_FLAME_HEAL;
                caster.GetAlliesWithinRange(targetList, Spell.SpellInfo.GetMaxRange());
            }
            else
            {
                spell = EvokerSpells.RED_LIVING_FLAME_DAMAGE;
                caster.GetEnemiesWithinRange(targetList, Spell.SpellInfo.GetMaxRange());
            }

            // reduce targetList to the number allowed
            targetList.Remove(spellTarget);
            targetList.RandomResize(aura.GetEffect(0).Amount);

            // cast on targets
            foreach (var target in targetList)
                caster.SpellFactory.CastSpell(target, spell, TriggerCastFlags.TriggeredAllowProc);

            aura.Remove();
        }
    }
}