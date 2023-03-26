// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps;

public class BattlegroundMap : Map
{
    private Battleground _bg;

	public BattlegroundMap(uint id, uint expiry, uint InstanceId, Difficulty spawnMode)
		: base(id, expiry, InstanceId, spawnMode)
	{
		InitVisibilityDistance();
	}

	public override void InitVisibilityDistance()
	{
		VisibleDistance = IsBattleArena ? Global.WorldMgr.MaxVisibleDistanceInArenas : Global.WorldMgr.MaxVisibleDistanceInBG;
		VisibilityNotifyPeriod = IsBattleArena ? Global.WorldMgr.VisibilityNotifyPeriodInArenas : Global.WorldMgr.VisibilityNotifyPeriodInBG;
	}

	public override TransferAbortParams CannotEnter(Player player)
	{
		if (player.Map == this)
		{
			Log.Logger.Error("BGMap:CannotEnter - player {0} is already in map!", player.GUID.ToString());

			return new TransferAbortParams(TransferAbortReason.Error);
		}

		if (player.BattlegroundId != InstanceId)
			return new TransferAbortParams(TransferAbortReason.LockedToDifferentInstance);

		return base.CannotEnter(player);
	}

	public override bool AddPlayerToMap(Player player, bool initPlayer = true)
	{
		player.InstanceValid = true;

		return base.AddPlayerToMap(player, initPlayer);
	}

	public override void RemovePlayerFromMap(Player player, bool remove)
	{
		Log.Logger.Information("MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map",
								player.GetName(),
								InstanceId,
								MapName);

		base.RemovePlayerFromMap(player, remove);
	}

	public void SetUnload()
	{
		UnloadTimer = 1;
	}

	public override void RemoveAllPlayers()
	{
		if (HavePlayers)
			foreach (var player in ActivePlayers)
				if (!player.IsBeingTeleportedFar)
					player.TeleportTo(player.BattlegroundEntryPoint);
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