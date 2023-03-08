// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Monk;

[SpellScript(117906)]
public class spell_monk_elusive_brawler_mastery : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.TypeMask.HasFlag(ProcFlags.TakenHitMask))
			return true;

		return eventInfo.ProcSpell && (eventInfo.ProcSpell.SpellInfo.Id == MonkSpells.BLACKOUT_STRIKE || eventInfo.ProcSpell.SpellInfo.Id == MonkSpells.BREATH_OF_FIRE);
	}
}