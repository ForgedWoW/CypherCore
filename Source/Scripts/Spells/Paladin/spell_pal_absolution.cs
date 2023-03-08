// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Paladin;

//212056
[Script]
public class spell_pal_absolution : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
	public Class PlayerClass { get; } = Class.Paladin;

	public spell_pal_absolution() : base("absolution") { }

	public void OnSpellCast(Player player, Spell spell, bool skipCheck)
	{
		if (player.Class != Class.Paladin)
			return;

		uint absolution = 212056;

		if (spell.SpellInfo.Id == absolution)
		{
			var allies = new List<Unit>();
			player.GetFriendlyUnitListInRange(allies, 30.0f, false);

			foreach (var targets in allies)
				if (targets.IsDead)
				{
					var playerTarget = targets.AsPlayer;

					if (playerTarget != null)
						playerTarget.ResurrectPlayer(0.35f, false);
				}
		}
	}
}