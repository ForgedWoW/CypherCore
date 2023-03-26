// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class MessageDistDelivererToHostile<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
    private readonly Unit _source;
    private readonly T _packetSender;
    private readonly PhaseShift _phaseShift;
    private readonly float _distSq;

	public GridType GridType { get; set; }

	public MessageDistDelivererToHostile(Unit src, T packetSender, float dist, GridType gridType)
	{
		_source = src;
		_packetSender = packetSender;
		_phaseShift = src.PhaseShift;
		_distSq = dist * dist;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if (creature.Location.GetExactDist2dSq(_source.Location) > _distSq)
				continue;

			// Send packet to all who are sharing the creature's vision
			if (creature.HasSharedVision)
				foreach (var player in creature.GetSharedVisionList())
					if (player.SeerView == creature)
						SendPacket(player);
		}
	}

	public void Visit(IList<DynamicObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var dynamicObject = objs[i];

			if (!dynamicObject.InSamePhase(_phaseShift))
				continue;

			if (dynamicObject.Location.GetExactDist2dSq(_source.Location) > _distSq)
				continue;

			var caster = dynamicObject.GetCaster();

			if (caster != null)
			{
				// Send packet back to the caster if the caster has vision of dynamic object
				var player = caster.AsPlayer;

				if (player && player.SeerView == dynamicObject)
					SendPacket(player);
			}
		}
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (!player.InSamePhase(_phaseShift))
				continue;

			if (player.Location.GetExactDist2dSq(_source.Location) > _distSq)
				continue;

			// Send packet to all who are sharing the player's vision
			if (player.HasSharedVision)
				foreach (var visionPlayer in player.GetSharedVisionList())
					if (visionPlayer.SeerView == player)
						SendPacket(visionPlayer);

			if (player.SeerView == player || player.Vehicle1)
				SendPacket(player);
		}
	}

    private void SendPacket(Player player)
	{
		// never send packet to self
		if (player == _source || !player.HaveAtClient(_source) || player.IsFriendlyTo(_source))
			return;

		_packetSender.Invoke(player);
	}
}