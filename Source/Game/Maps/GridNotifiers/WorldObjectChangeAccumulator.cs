// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class WorldObjectChangeAccumulator : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierDynamicObject
{
	readonly Dictionary<Player, UpdateData> _updateData;
	readonly WorldObject _worldObject;
	readonly List<ObjectGuid> _plrList = new();

	public GridType GridType { get; set; }

	public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d, GridType gridType)
	{
		_updateData = d;
		_worldObject = obj;
		GridType = gridType;
	}

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

	void BuildPacket(Player player)
	{
		// Only send update once to a player
		if (!_plrList.Contains(player.GUID) && player.HaveAtClient(_worldObject))
		{
			_worldObject.BuildFieldsUpdate(player, _updateData);
			_plrList.Add(player.GUID);
		}
	}
}