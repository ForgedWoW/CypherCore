// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH, EvokerSpells.RED_FIRE_BREATH_2)]
internal class spell_evoker_leaping_flames : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        if (Caster.HasSpell(EvokerSpells.LEAPING_FLAMES))
        {
            var auraEff = Caster.AddAura(EvokerSpells.LEAPING_FLAMES_AURA).GetEffect(0);
            auraEff.SetAmount(auraEff.Amount + stage.Stage);
        }
    }
}