﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_defender_of_azeroth_death_gate_selector : SpellScript, IHasSpellEffects
{
	private readonly (WorldLocation, uint) OrgrimmarInnLoc = (new WorldLocation(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);
	private readonly (WorldLocation, uint) StormwindInnLoc = (new WorldLocation(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var player = HitUnit.AsPlayer;

		if (player == null)
			return;

		if (player.GetQuestStatus(QuestIds.DefenderOfAzerothAlliance) == QuestStatus.None &&
			player.GetQuestStatus(QuestIds.DefenderOfAzerothHorde) == QuestStatus.None)
			return;

		(WorldLocation Loc, uint AreaId) bindLoc = player.Team == TeamFaction.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
		player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
		player.SendBindPointUpdate();
		player.SendPlayerBound(player.GUID, bindLoc.AreaId);

		player.CastSpell(player, player.Team == TeamFaction.Alliance ? GenericSpellIds.DeathGateTeleportStormwind : GenericSpellIds.DeathGateTeleportOrgrimmar);
	}
}