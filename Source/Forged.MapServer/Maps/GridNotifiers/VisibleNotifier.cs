// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class VisibleNotifier : IGridNotifierWorldObject
{
	public GridType GridType { get; set; }
	internal Player Player { get; set; }
	internal UpdateData Data { get; set; }
	internal List<ObjectGuid> VisGuids { get; set; }
	internal List<Unit> VisibleNow { get; set; }

	public VisibleNotifier(Player pl, GridType gridType)
	{
		Player = pl;
		Data = new UpdateData(pl.Location.MapId);
		VisGuids = new List<ObjectGuid>(pl.ClientGuiDs);
		VisibleNow = new List<Unit>();
		GridType = gridType;
	}

	public void Visit(IList<WorldObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var obj = objs[i];

			VisGuids.Remove(obj.GUID);
			Player.UpdateVisibilityOf(obj, Data, VisibleNow);
		}
	}

	public void SendToSelf()
	{
		// at this moment i_clientGUIDs have guids that not iterate at grid level checks
		// but exist one case when this possible and object not out of range: transports
		var transport = Player.GetTransport<Transport>();

		if (transport)
			foreach (var obj in transport.GetPassengers())
				if (VisGuids.Contains(obj.GUID))
				{
					VisGuids.Remove(obj.GUID);

					switch (obj.TypeId)
					{
						case TypeId.GameObject:
							Player.UpdateVisibilityOf(obj.AsGameObject, Data, VisibleNow);

							break;
						case TypeId.Player:
							Player.UpdateVisibilityOf(obj.AsPlayer, Data, VisibleNow);

							if (!obj.IsNeedNotify(NotifyFlags.VisibilityChanged))
								obj.AsPlayer.UpdateVisibilityOf(Player);

							break;
						case TypeId.Unit:
							Player.UpdateVisibilityOf(obj.AsCreature, Data, VisibleNow);

							break;
						case TypeId.DynamicObject:
							Player.UpdateVisibilityOf(obj.AsDynamicObject, Data, VisibleNow);

							break;
						case TypeId.AreaTrigger:
							Player.UpdateVisibilityOf(obj.AsAreaTrigger, Data, VisibleNow);

							break;
						default:
							break;
					}
				}

		foreach (var guid in VisGuids)
		{
			Player.ClientGuiDs.Remove(guid);
			Data.AddOutOfRangeGUID(guid);

			if (guid.IsPlayer)
			{
				var pl = Global.ObjAccessor.FindPlayer(guid);

				if (pl != null && pl.IsInWorld && !pl.IsNeedNotify(NotifyFlags.VisibilityChanged))
					pl.UpdateVisibilityOf(Player);
			}
		}

		if (!Data.HasData())
			return;

		Data.BuildPacket(out var packet);
		Player.SendPacket(packet);

		foreach (var obj in VisibleNow)
			Player.SendInitialVisiblePackets(obj);
	}
}