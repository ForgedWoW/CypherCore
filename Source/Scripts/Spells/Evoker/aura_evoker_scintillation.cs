// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.SCINTILLATION)]
public class aura_evoker_scintillation : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id.EqualsAny(EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2) && RandomHelper.randChance(Aura.GetEffect(1).Amount);
    }

    public void OnProc(ProcEventInfo info)
    {
        CastSpellExtraArgs args = new(true);
        args.EmpowerStage = 1;
        args.TriggeringAura = Aura.GetEffect(0);
        Caster.CastSpell(info.ActionTarget, EvokerSpells.ETERNITY_SURGE_CHARGED, args);
    }
}