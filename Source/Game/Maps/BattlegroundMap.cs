// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;

namespace Game.Maps;

public class BattlegroundMap : Map
{
	Battleground _bg;

	public BattlegroundMap(uint id, uint expiry, uint InstanceId, Difficulty spawnMode)
		: base(id, expiry, InstanceId, spawnMode)
	{
		InitVisibilityDistance();
	}

	public override void InitVisibilityDistance()
	{
		VisibleDistance        = IsBattleArena() ? Global.WorldMgr.GetMaxVisibleDistanceInArenas() : Global.WorldMgr.GetMaxVisibleDistanceInBG();
		VisibilityNotifyPeriod = IsBattleArena() ? Global.WorldMgr.GetVisibilityNotifyPeriodInArenas() : Global.WorldMgr.GetVisibilityNotifyPeriodInBG();
	}

	public override TransferAbortParams CannotEnter(Player player)
	{
		if (player.GetMap() == this)
		{
			Log.outError(LogFilter.Maps, "BGMap:CannotEnter - player {0} is already in map!", player.GetGUID().ToString());
			Cypher.Assert(false);

			return new TransferAbortParams(TransferAbortReason.Error);
		}

		if (player.GetBattlegroundId() != GetInstanceId())
			return new TransferAbortParams(TransferAbortReason.LockedToDifferentInstance);

		return base.CannotEnter(player);
	}

	public override bool AddPlayerToMap(Player player, bool initPlayer = true)
	{
		player.m_InstanceValid = true;

		return base.AddPlayerToMap(player, initPlayer);
	}

	public override void RemovePlayerFromMap(Player player, bool remove)
	{
		Log.outInfo(LogFilter.Maps,
		            "MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map",
		            player.GetName(),
		            GetInstanceId(),
		            GetMapName());

		base.RemovePlayerFromMap(player, remove);
	}

	public void SetUnload()
	{
		UnloadTimer = 1;
	}

	public override void RemoveAllPlayers()
	{
		if (HavePlayers())
			foreach (var player in ActivePlayers)
				if (!player.IsBeingTeleportedFar())
					player.TeleportTo(player.GetBattlegroundEntryPoint());
	}

	public Battleground GetBG()
	{
		return _bg;
	}

	public void SetBG(Battleground bg)
	{
		_bg = bg;
	}
}