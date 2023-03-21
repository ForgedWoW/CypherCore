// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.BattleFields;

namespace Forged.RealmServer.Entities;

public class SpellArea
{
	public uint SpellId;
	public uint AreaId;           // zone/subzone/or 0 is not limited to zone
	public uint QuestStart;       // quest start (quest must be active or rewarded for spell apply)
	public uint QuestEnd;         // quest end (quest must not be rewarded for spell apply)
	public int AuraSpell;         // spell aura must be applied for spell apply)if possitive) and it must not be applied in other case
	public ulong RaceMask;        // can be applied only to races
	public Gender Gender;         // can be applied only to gender
	public uint QuestStartStatus; // QuestStatus that quest_start must have in order to keep the spell
	public uint QuestEndStatus;   // QuestStatus that the quest_end must have in order to keep the spell (if the quest_end's status is different than this, the spell will be dropped)
	public SpellAreaFlag Flags;   // if SPELL_AREA_FLAG_AUTOCAST then auto applied at area enter, in other case just allowed to cast || if SPELL_AREA_FLAG_AUTOREMOVE then auto removed inside area (will allways be removed on leaved even without flag)

	// helpers
	public bool IsFitToRequirements(Player player, uint newZone, uint newArea)
	{
		if (Gender != Gender.None) // not in expected gender
			if (player == null || Gender != player.NativeGender)
				return false;

		if (RaceMask != 0) // not in expected race
			if (player == null || !Convert.ToBoolean(RaceMask & (ulong)SharedConst.GetMaskForRace(player.Race)))
				return false;

		if (AreaId != 0) // not in expected zone
			if (newZone != AreaId && newArea != AreaId)
				return false;

		if (QuestStart != 0) // not in expected required quest state
			if (player == null || (((1 << (int)player.GetQuestStatus(QuestStart)) & QuestStartStatus) == 0))
				return false;

		if (QuestEnd != 0) // not in expected forbidden quest state
			if (player == null || (((1 << (int)player.GetQuestStatus(QuestEnd)) & QuestEndStatus) == 0))
				return false;

		if (AuraSpell != 0) // not have expected aura
			if (player == null || (AuraSpell > 0 && !player.HasAura((uint)AuraSpell)) || (AuraSpell < 0 && player.HasAura((uint)-AuraSpell)))
				return false;

		if (player)
		{
			var bg = player.Battleground;

			if (bg)
				return bg.IsSpellAllowed(SpellId, player);
		}

		// Extra conditions -- leaving the possibility add extra conditions...
		switch (SpellId)
		{
			case 91604: // No fly Zone - Wintergrasp
			{
				if (!player)
					return false;

				var Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.Map, player.Zone);

				if (Bf == null || Bf.CanFlyIn() || (!player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !player.HasAuraType(AuraType.Fly)))
					return false;

				break;
			}
			case 56618: // Horde Controls Factory Phase Shift
			case 56617: // Alliance Controls Factory Phase Shift
			{
				if (!player)
					return false;

				var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.Map, player.Zone);

				if (bf == null || bf.GetTypeId() != (int)BattleFieldTypes.WinterGrasp)
					return false;

				// team that controls the workshop in the specified area
				var team = bf.GetData(newArea);

				if (team == TeamIds.Horde)
					return SpellId == 56618;
				else if (team == TeamIds.Alliance)
					return SpellId == 56617;

				break;
			}
			case 57940: // Essence of Wintergrasp - Northrend
			case 58045: // Essence of Wintergrasp - Wintergrasp
			{
				if (!player)
					return false;

				var battlefieldWG = Global.BattleFieldMgr.GetBattlefieldByBattleId(player.Map, 1);

				if (battlefieldWG != null)
					return battlefieldWG.IsEnabled() && (player.TeamId == battlefieldWG.GetDefenderTeam()) && !battlefieldWG.IsWarTime();

				break;
			}
			case 74411: // Battleground- Dampening
			{
				if (!player)
					return false;

				var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.Map, player.Zone);

				if (bf != null)
					return bf.IsWarTime();

				break;
			}
		}

		return true;
	}
}