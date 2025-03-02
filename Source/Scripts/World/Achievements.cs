// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAchievement;

namespace Scripts.World.Achievements;

internal struct AreaIds
{
	//Tilted
	public const uint AreaArgentTournamentFields = 4658;
	public const uint AreaRingOfAspirants = 4670;
	public const uint AreaRingOfArgentValiants = 4671;
	public const uint AreaRingOfAllianceValiants = 4672;
	public const uint AreaRingOfHordeValiants = 4673;
	public const uint AreaRingOfChampions = 4669;
}

internal struct AuraIds
{
	//Flirt With Disaster
	public const uint AuraPerfumeForever = 70235;
	public const uint AuraPerfumeEnchantress = 70234;
	public const uint AuraPerfumeVictory = 70233;
}

internal struct VehicleIds
{
	//BgSA Artillery
	public const uint AntiPersonnalCannon = 27894;
}

[Script("achievement_arena_2v2_kills", ArenaTypes.Team2v2)]
[Script("achievement_arena_3v3_kills", ArenaTypes.Team3v3)]
[Script("achievement_arena_5v5_kills", ArenaTypes.Team5v5)]
internal class achievement_arena_kills : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
	private readonly ArenaTypes _arenaType;

	public achievement_arena_kills(string name, ArenaTypes arenaType) : base(name)
	{
		_arenaType = arenaType;
	}

	public bool OnCheck(Player source, Unit target)
	{
		// this checks GetBattleground() for Null already
		if (!source.InArena)
			return false;

		return source.Battleground.GetArenaType() == _arenaType;
	}
}

[Script]
internal class achievement_tilted : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
	public achievement_tilted() : base("achievement_tilted") { }

	public bool OnCheck(Player player, Unit target)
	{
		if (!player)
			return false;

		var checkArea = player.Area == AreaIds.AreaArgentTournamentFields ||
						player.Area == AreaIds.AreaRingOfAspirants ||
						player.Area == AreaIds.AreaRingOfArgentValiants ||
						player.Area == AreaIds.AreaRingOfAllianceValiants ||
						player.Area == AreaIds.AreaRingOfHordeValiants ||
						player.Area == AreaIds.AreaRingOfChampions;

		return checkArea && player.Duel != null && player.Duel.IsMounted;
	}
}

[Script]
internal class achievement_flirt_with_disaster_perf_check : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
	public achievement_flirt_with_disaster_perf_check() : base("achievement_flirt_with_disaster_perf_check") { }

	public bool OnCheck(Player player, Unit target)
	{
		if (!player)
			return false;

		if (player.HasAura(AuraIds.AuraPerfumeForever) ||
			player.HasAura(AuraIds.AuraPerfumeEnchantress) ||
			player.HasAura(AuraIds.AuraPerfumeVictory))
			return true;

		return false;
	}
}

[Script]
internal class achievement_killed_exp_or_honor_target : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
	public achievement_killed_exp_or_honor_target() : base("achievement_killed_exp_or_honor_target") { }

	public bool OnCheck(Player player, Unit target)
	{
		return target && player.IsHonorOrXPTarget(target);
	}
}

[Script] // 7433 - Newbie
internal class achievement_newbie : ScriptObjectAutoAddDBBound, IAchievementOnCompleted
{
	public achievement_newbie() : base("achievement_newbie") { }

	public void OnCompleted(Player player, AchievementRecord achievement)
	{
		player.Session.BattlePetMgr.UnlockSlot(BattlePetSlots.Slot1);
		// TODO: Unlock trap
	}
}

[Script] // 6566 - Just a Pup
internal class achievement_just_a_pup : ScriptObjectAutoAddDBBound, IAchievementOnCompleted
{
	public achievement_just_a_pup() : base("achievement_just_a_pup") { }

	public void OnCompleted(Player player, AchievementRecord achievement)
	{
		player.Session.BattlePetMgr.UnlockSlot(BattlePetSlots.Slot2);
	}
}