// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class MessageDistDeliverer<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
	readonly WorldObject _source;
	readonly T _packetSender;
	readonly PhaseShift _phaseShift;
	readonly float _distSq;
	readonly Team _team;
	readonly Player _skippedReceiver;
	readonly bool _required3dDist;

	public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
	{
		_source       = src;
		_packetSender = packetSender;
		_phaseShift   = src.GetPhaseShift();
		_distSq       = dist * dist;

		if (own_team_only && src.IsPlayer())
			_team = src.ToPlayer().GetEffectiveTeam();

		_skippedReceiver = skipped;
		_required3dDist   = req3dDist;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if ((!_required3dDist ? creature.GetExactDist2dSq(_source) : creature.GetExactDistSq(_source)) > _distSq)
				continue;

			// Send packet to all who are sharing the creature's vision
			if (creature.HasSharedVision())
				foreach (var visionPlayer in creature.GetSharedVisionList())
					if (visionPlayer.seerView == creature)
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

			if ((!_required3dDist ? dynamicObject.GetExactDist2dSq(_source) : dynamicObject.GetExactDistSq(_source)) > _distSq)
				continue;

			// Send packet back to the caster if the caster has vision of dynamic object
			var caster = dynamicObject.GetCaster();

			if (caster)
			{
				var player = caster.ToPlayer();

				if (player && player.seerView == dynamicObject)
					SendPacket(player);
			}
		}
	}

	public GridType GridType { get; set; } = GridType.World;

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (!player.InSamePhase(_phaseShift))
				continue;

			if ((!_required3dDist ? player.GetExactDist2dSq(_source) : player.GetExactDistSq(_source)) > _distSq)
				continue;

			// Send packet to all who are sharing the player's vision
			if (player.HasSharedVision())
				foreach (var visionPlayer in player.GetSharedVisionList())
					if (visionPlayer.seerView == player)
						SendPacket(visionPlayer);

			if (player.seerView == player || player.GetVehicle() != null)
				SendPacket(player);
		}
	}

	void SendPacket(Player player)
	{
		// never send packet to self
		if (_source == player || (_team != 0 && player.GetEffectiveTeam() != _team) || _skippedReceiver == player)
			return;

		if (!player.HaveAtClient(_source))
			return;

		_packetSender.Invoke(player);
	}
}