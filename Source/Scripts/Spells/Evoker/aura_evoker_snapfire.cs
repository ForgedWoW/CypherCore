// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.SNAPFIRE)]
public class aura_evoker_snapfire : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id.EqualsAny(EvokerSpells.RED_LIVING_FLAME_DAMAGE, EvokerSpells.RED_LIVING_FLAME_HEAL) && RandomHelper.randChance(Aura.GetEffect(0).Amount);
    }
}