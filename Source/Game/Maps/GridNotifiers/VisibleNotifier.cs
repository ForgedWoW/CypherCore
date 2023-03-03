// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;
using Game.Networking.Packets;

namespace Game.Maps
{
    public class VisibleNotifier : IGridNotifierWorldObject
    {
        public GridType GridType { get; set; }
        public VisibleNotifier(Player pl, GridType gridType)
        {
            i_player = pl;
            i_data = new UpdateData(pl.GetMapId());
            vis_guids = new List<ObjectGuid>(pl.m_clientGUIDs);
            i_visibleNow = new List<Unit>();
            GridType = gridType;
        }

        public void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                vis_guids.Remove(obj.GetGUID());
                i_player.UpdateVisibilityOf(obj, i_data, i_visibleNow);
            }
        }

        public void SendToSelf()
        {
            // at this moment i_clientGUIDs have guids that not iterate at grid level checks
            // but exist one case when this possible and object not out of range: transports
            Transport transport = i_player.GetTransport<Transport>();
            if (transport)
            {
                foreach (var obj in transport.GetPassengers())
                {
                    if (vis_guids.Contains(obj.GetGUID()))
                    {
                        vis_guids.Remove(obj.GetGUID());

                        switch (obj.GetTypeId())
                        {
                            case TypeId.GameObject:
                                i_player.UpdateVisibilityOf(obj.ToGameObject(), i_data, i_visibleNow);
                                break;
                            case TypeId.Player:
                                i_player.UpdateVisibilityOf(obj.ToPlayer(), i_data, i_visibleNow);
                                if (!obj.IsNeedNotify(NotifyFlags.VisibilityChanged))
                                    obj.ToPlayer().UpdateVisibilityOf(i_player);
                                break;
                            case TypeId.Unit:
                                i_player.UpdateVisibilityOf(obj.ToCreature(), i_data, i_visibleNow);
                                break;
                            case TypeId.DynamicObject:
                                i_player.UpdateVisibilityOf(obj.ToDynamicObject(), i_data, i_visibleNow);
                                break;
                            case TypeId.AreaTrigger:
                                i_player.UpdateVisibilityOf(obj.ToAreaTrigger(), i_data, i_visibleNow);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            foreach (var guid in vis_guids)
            {
                i_player.m_clientGUIDs.Remove(guid);
                i_data.AddOutOfRangeGUID(guid);

                if (guid.IsPlayer())
                {
                    Player pl = Global.ObjAccessor.FindPlayer(guid);
                    if (pl != null && pl.IsInWorld && !pl.IsNeedNotify(NotifyFlags.VisibilityChanged))
                        pl.UpdateVisibilityOf(i_player);
                }
            }

            if (!i_data.HasData())
                return;

            i_data.BuildPacket(out UpdateObject packet);
            i_player.SendPacket(packet);

            foreach (var obj in i_visibleNow)
                i_player.SendInitialVisiblePackets(obj);
        }

        internal Player i_player;
        internal UpdateData i_data;
        internal List<ObjectGuid> vis_guids;
        internal List<Unit> i_visibleNow;
    }
}