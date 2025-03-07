﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Druid;

[Script]
public class feral_spells : ScriptObjectAutoAdd, IPlayerOnLogin
{
	public PlayerClass PlayerClass
	{
		get { return PlayerClass.Druid; }
	}

	public feral_spells() : base("feral_spells") { }

	public void OnLogin(Player player)
	{
		if (player.GetPrimarySpecialization() != TalentSpecialization.DruidCat)
			return;

		if (player.Level >= 5 && !player.HasSpell(DruidSpells.SHRED))
			player.LearnSpell(DruidSpells.SHRED, false);

		if (player.Level >= 20 && !player.HasSpell(DruidSpells.RIP))
			player.LearnSpell(DruidSpells.RIP, false);

		if (player.Level >= 24 && !player.HasSpell(DruidSpells.RAKE))
			player.LearnSpell(DruidSpells.RAKE, false);

		if (player.Level >= 32 && !player.HasSpell(DruidSpells.FEROCIOUS_BITE))
			player.LearnSpell(DruidSpells.FEROCIOUS_BITE, false);
	}
}