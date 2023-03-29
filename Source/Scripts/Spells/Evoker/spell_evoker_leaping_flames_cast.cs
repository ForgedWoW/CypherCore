// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME)]
internal class spell_evoker_leaping_flames_cast : SpellScript, ISpellOnCast
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
                caster.CastSpell(target, spell, TriggerCastFlags.TriggeredAllowProc);

            aura.Remove();
        }
    }
}