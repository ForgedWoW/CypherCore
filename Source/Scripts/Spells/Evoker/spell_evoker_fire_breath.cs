// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH, EvokerSpells.RED_FIRE_BREATH_2)]
internal class spell_evoker_fire_breath : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        CastSpellExtraArgs args = new(TriggerCastFlags.TriggeredAllowProc);
        args.EmpowerStage = stage.Stage;
        args.AddSpellMod(SpellValueMod.BasePoint3, Caster.AsPlayer.HasSpell(EvokerSpells.SCOURING_FLAME) ? stage.Stage : 0);
        Caster.CastSpell(new CastSpellTargetArg(), EvokerSpells.RED_FIRE_BREATH_CHARGED, args);
    }
}