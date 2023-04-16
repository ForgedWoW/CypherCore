// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.World;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Maps;

public class BattlegroundMap : Map
{
    private readonly WorldManager _worldManager;

    public BattlegroundMap(uint id, uint expiry, uint instanceId, Difficulty spawnMode, ClassFactory classFactory)
        : base(id, expiry, instanceId, spawnMode, classFactory)
    {
        _worldManager = classFactory.Resolve<WorldManager>();
        InitVisibilityDistance();
    }

    public Battleground BG { get; private set; }

    public override bool AddPlayerToMap(Player player, bool initPlayer = true)
    {
        player.InstanceValid = true;

        return base.AddPlayerToMap(player, initPlayer);
    }

    public override TransferAbortParams CannotEnter(Player player)
    {
        if (player.Location.Map != this)
            return player.BattlegroundId != InstanceId ? new TransferAbortParams(TransferAbortReason.LockedToDifferentInstance) : base.CannotEnter(player);

        Log.Logger.Error("BGMap:CannotEnter - player {0} is already in map!", player.GUID.ToString());

        return new TransferAbortParams(TransferAbortReason.Error);
    }

    public override void InitVisibilityDistance()
    {
        VisibleDistance = IsBattleArena ? _worldManager.MaxVisibleDistanceInArenas : _worldManager.MaxVisibleDistanceInBG;
        VisibilityNotifyPeriod = IsBattleArena ? _worldManager.VisibilityNotifyPeriodInArenas : _worldManager.VisibilityNotifyPeriodInBG;
    }

    public override void RemoveAllPlayers()
    {
        if (!HavePlayers)
            return;

        foreach (var player in ActivePlayers.Where(player => !player.IsBeingTeleportedFar))
            player.TeleportTo(player.BattlegroundEntryPoint);
    }

    public override void RemovePlayerFromMap(Player player, bool remove)
    {
        Log.Logger.Information("MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map",
                               player.GetName(),
                               InstanceId,
                               MapName);

        base.RemovePlayerFromMap(player, remove);
    }

    public void SetBG(Battleground bg)
    {
        BG = bg;
    }

    public void SetUnload()
    {
        UnloadTimer = 1;
    }
}