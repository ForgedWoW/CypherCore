// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Bgs.Protocol.Notification.V1;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.TEMPORAL_COMPRESSION)]
public class aura_evoker_temporal_compression : AuraScript, IAuraCheckProc, IAuraOnProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		return info.SpellInfo.Labels.Contains(EvokerLabels.BRONZE);
	}

    public void OnProc(ProcEventInfo info)
    {
		Caster.AddAura(EvokerSpells.TEMPORAL_COMPRESSION_AURA);
    }
}