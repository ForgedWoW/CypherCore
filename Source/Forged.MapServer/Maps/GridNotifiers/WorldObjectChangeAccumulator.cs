// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class WorldObjectChangeAccumulator : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierDynamicObject
{
    private readonly List<ObjectGuid> _plrList = new();
    private readonly Dictionary<Player, UpdateData> _updateData;
    private readonly WorldObject _worldObject;
    public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d, GridType gridType)
    {
        _updateData = d;
        _worldObject = obj;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (!creature.GetSharedVisionList().Empty())
                foreach (var visionPlayer in creature.GetSharedVisionList())
                    BuildPacket(visionPlayer);
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var dynamicObject = objs[i];

            var guid = dynamicObject.GetCasterGUID();

            if (guid.IsPlayer)
            {
                //Caster may be NULL if DynObj is in removelist
                var caster = Global.ObjAccessor.FindPlayer(guid);

                if (caster != null)
                    if (caster.ActivePlayerData.FarsightObject == dynamicObject.GUID)
                        BuildPacket(caster);
            }
        }
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];
            BuildPacket(player);

            if (!player.GetSharedVisionList().Empty())
                foreach (var visionPlayer in player.GetSharedVisionList())
                    BuildPacket(visionPlayer);
        }
    }

    private void BuildPacket(Player player)
    {
        // Only send update once to a player
        if (!_plrList.Contains(player.GUID) && player.HaveAtClient(_worldObject))
        {
            _worldObject.BuildFieldsUpdate(player, _updateData);
            _plrList.Add(player.GUID);
        }
    }
}