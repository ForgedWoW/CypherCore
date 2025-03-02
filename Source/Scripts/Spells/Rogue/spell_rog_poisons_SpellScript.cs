﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[]
{
	3409, 8679, 108211
})]
public class spell_rog_poisons_SpellScript : SpellScript, ISpellBeforeHit
{
	public void BeforeHit(SpellMissInfo missInfo)
	{
		if (missInfo != SpellMissInfo.None)
			return;

		var _player = Caster.AsPlayer;

		if (_player != null)
			RemovePreviousPoisons();
	}

	private void RemovePreviousPoisons()
	{
		var plr = Caster.AsPlayer;

		if (plr != null)
		{
			if (plr.HasAura(ePoisons.WoundPoison))
				plr.RemoveAura(ePoisons.WoundPoison);

			if (plr.HasAura(ePoisons.MindNumbingPoison))
				plr.RemoveAura(ePoisons.MindNumbingPoison);

			if (plr.HasAura(ePoisons.CripplingPoison))
				plr.RemoveAura(ePoisons.CripplingPoison);

			if (plr.HasAura(ePoisons.LeechingPoison))
				plr.RemoveAura(ePoisons.LeechingPoison);

			if (plr.HasAura(ePoisons.ParalyticPoison))
				plr.RemoveAura(ePoisons.ParalyticPoison);

			if (plr.HasAura(ePoisons.DeadlyPoison))
				plr.RemoveAura(ePoisons.DeadlyPoison);

			if (plr.HasAura(ePoisons.InstantPoison))
				plr.RemoveAura(ePoisons.InstantPoison);
		}
	}
}