// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Garrisons;

internal class GarrisonMap : Map
{
    private readonly ObjectGuid _owner;
    private Player _loadingPlayer; // @workaround Player is not registered in ObjectAccessor during login

    public GarrisonMap(uint id, long expiry, uint instanceId, ObjectGuid owner, ClassFactory classFactory) : base(id, expiry, instanceId, Difficulty.Normal, classFactory)
    {
        _owner = owner;
        InitVisibilityDistance();
    }

    public override bool AddPlayerToMap(Player player, bool initPlayer = true)
    {
        if (player.GUID == _owner)
            _loadingPlayer = player;

        var result = base.AddPlayerToMap(player, initPlayer);

        if (player.GUID == _owner)
            _loadingPlayer = null;

        return result;
    }

    public Garrison GetGarrison()
    {
        if (_loadingPlayer != null)
            return _loadingPlayer.Garrison;

        var owner = ObjectAccessor.FindConnectedPlayer(_owner);

        return owner?.Garrison;
    }

    public sealed override void InitVisibilityDistance()
    {
        //init visibility distance for instances
        VisibleDistance = WorldManager.MaxVisibleDistanceInInstances;
        VisibilityNotifyPeriod = WorldManager.VisibilityNotifyPeriodInInstances;
    }

    public override void LoadGridObjects(Grid grid, Cell cell)
    {
        base.LoadGridObjects(grid, cell);

        GarrisonGridLoader loader = new(grid, this, cell);
        loader.LoadN();
    }
}