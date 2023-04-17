// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH, EvokerSpells.RED_FIRE_BREATH_2)]
internal class SpellEvokerFireBreath : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        CastSpellExtraArgs args = new(TriggerCastFlags.TriggeredAllowProc);
        args.EmpowerStage = stage.Stage;
        args.AddSpellMod(SpellValueMod.BasePoint3, Caster.AsPlayer.HasSpell(EvokerSpells.SCOURING_FLAME) ? stage.Stage : 0);
        Caster.SpellFactory.CastSpell(new CastSpellTargetArg(), EvokerSpells.RED_FIRE_BREATH_CHARGED, args);
    }
}