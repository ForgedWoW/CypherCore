using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class WorldObjectChangeAccumulator : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierDynamicObject
{
    public GridType GridType { get; set; }
    public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d, GridType gridType)
    {
        updateData = d;
        worldObject = obj;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            BuildPacket(player);

            if (!player.GetSharedVisionList().Empty())
            {
                foreach (var visionPlayer in player.GetSharedVisionList())
                    BuildPacket(visionPlayer);
            }
        }
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.GetSharedVisionList().Empty())
            {
                foreach (var visionPlayer in creature.GetSharedVisionList())
                    BuildPacket(visionPlayer);
            }
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            DynamicObject dynamicObject = objs[i];

            ObjectGuid guid = dynamicObject.GetCasterGUID();
            if (guid.IsPlayer())
            {
                //Caster may be NULL if DynObj is in removelist
                Player caster = Global.ObjAccessor.FindPlayer(guid);
                if (caster != null)
                    if (caster.m_activePlayerData.FarsightObject == dynamicObject.GetGUID())
                        BuildPacket(caster);
            }
        }
    }

    void BuildPacket(Player player)
    {
        // Only send update once to a player
        if (!plr_list.Contains(player.GetGUID()) && player.HaveAtClient(worldObject))
        {
            worldObject.BuildFieldsUpdate(player, updateData);
            plr_list.Add(player.GetGUID());
        }
    }

    readonly Dictionary<Player, UpdateData> updateData;
    readonly WorldObject worldObject;
    readonly List<ObjectGuid> plr_list = new();
}