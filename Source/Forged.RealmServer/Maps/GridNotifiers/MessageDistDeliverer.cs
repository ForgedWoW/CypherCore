// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

public class MessageDistDeliverer<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
	readonly WorldObject _source;
	readonly T _packetSender;
	readonly PhaseShift _phaseShift;
	readonly float _distSq;
	readonly TeamFaction _team;
	readonly Player _skippedReceiver;
	readonly bool _required3dDist;

	public GridType GridType { get; set; } = GridType.World;

	public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
	{
		_source = src;
		_packetSender = packetSender;
		_phaseShift = src.PhaseShift;
		_distSq = dist * dist;

		if (own_team_only && src.IsPlayer)
			_team = src.AsPlayer.EffectiveTeam;

		_skippedReceiver = skipped;
		_required3dDist = req3dDist;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if ((!_required3dDist ? creature.Location.GetExactDist2dSq(_source.Location) : creature.Location.GetExactDistSq(_source.Location)) > _distSq)
				continue;

			// Send packet to all who are sharing the creature's vision
			if (creature.HasSharedVision)
				foreach (var visionPlayer in creature.GetSharedVisionList())
					if (visionPlayer.SeerView == creature)
						SendPacket(visionPlayer);
		}
	}

	public void Visit(IList<DynamicObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var dynamicObject = objs[i];

			if (!dynamicObject.InSamePhase(_phaseShift))
				continue;

			if ((!_required3dDist ? dynamicObject.Location.GetExactDist2dSq(_source.Location) : dynamicObject.Location.GetExactDistSq(_source.Location)) > _distSq)
				continue;

			// Send packet back to the caster if the caster has vision of dynamic object
			var caster = dynamicObject.GetCaster();

			if (caster)
			{
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

			if ((!_required3dDist ? player.Location.GetExactDist2dSq(_source.Location) : player.Location.GetExactDistSq(_source.Location)) > _distSq)
				continue;

			// Send packet to all who are sharing the player's vision
			if (player.HasSharedVision)
				foreach (var visionPlayer in player.GetSharedVisionList())
					if (visionPlayer.SeerView == player)
						SendPacket(visionPlayer);

			if (player.SeerView == player || player.Vehicle1 != null)
				SendPacket(player);
		}
	}

	void SendPacket(Player player)
	{
		// never send packet to self
		if (_source == player || (_team != 0 && player.EffectiveTeam != _team) || _skippedReceiver == player)
			return;

		if (!player.HaveAtClient(_source))
			return;

		_packetSender.Invoke(player);
	}
}