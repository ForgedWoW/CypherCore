// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using static Game.AI.PlayerAI;
using System.Collections.Generic;
using Framework.Constants;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH, EvokerSpells.RED_FIRE_BREATH_2)]
internal class spell_evoker_life_givers_flame_cast : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster.TryGetAura(EvokerSpells.LIFE_GIVERS_FLAME_AURA, out var aura))
        {
            // get targets
            var targetList = new List<Unit>();
            caster.GetAlliesWithinRange(targetList, Spell.SpellInfo.GetMaxRange());

            // only if injured
            targetList.RemoveIf(a => a.IsFullHealth);

            // reduce targetList to the number allowed
            targetList.RandomResize(1);

            // cast on targets
            var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc);
            args.AddSpellMod(SpellValueMod.BasePoint0, aura.GetEffect(0).Amount * (SpellManager.Instance.GetSpellInfo(EvokerSpells.LIFE_GIVERS_FLAME).GetEffect(0).BasePoints * 0.01));
            foreach (var target in targetList)
                caster.CastSpell(target, EvokerSpells.LIFE_GIVERS_FLAME_HEAL, args);

            aura.Remove();
        }
    }
}