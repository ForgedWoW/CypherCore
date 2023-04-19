// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class MessageDistDeliverer<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
    private readonly float _distSq;
    private readonly T _packetSender;
    private readonly PhaseShift _phaseShift;
    private readonly bool _required3dDist;
    private readonly Player _skippedReceiver;
    private readonly WorldObject _source;
    private readonly TeamFaction _team;

    public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool ownTeamOnly = false, Player skipped = null, bool req3dDist = false)
    {
        _source = src;
        _packetSender = packetSender;
        _phaseShift = src.Location.PhaseShift;
        _distSq = dist * dist;

        if (ownTeamOnly && src.IsPlayer)
            _team = src.AsPlayer.EffectiveTeam;

        _skippedReceiver = skipped;
        _required3dDist = req3dDist;
    }

    public GridType GridType { get; set; } = GridType.World;

    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if ((!_required3dDist ? creature.Location.GetExactDist2dSq(_source.Location) : creature.Location.GetExactDistSq(_source.Location)) > _distSq)
                continue;

            // Send packet to all who are sharing the creature's vision
            if (!creature.HasSharedVision)
                continue;

            foreach (var visionPlayer in creature.GetSharedVisionList().Where(visionPlayer => visionPlayer.SeerView == creature))
                SendPacket(visionPlayer);
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        foreach (var dynamicObject in objs)
        {
            if (!dynamicObject.Location.InSamePhase(_phaseShift))
                continue;

            if ((!_required3dDist ? dynamicObject.Location.GetExactDist2dSq(_source.Location) : dynamicObject.Location.GetExactDistSq(_source.Location)) > _distSq)
                continue;

            // Send packet back to the caster if the caster has vision of dynamic object
            var player = dynamicObject.Caster?.AsPlayer;

            if (player == null)
                continue;

            if (player.SeerView == dynamicObject)
                SendPacket(player);
        }
    }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            if (!player.Location.InSamePhase(_phaseShift))
                continue;

            if ((!_required3dDist ? player.Location.GetExactDist2dSq(_source.Location) : player.Location.GetExactDistSq(_source.Location)) > _distSq)
                continue;

            // Send packet to all who are sharing the player's vision
            if (player.HasSharedVision)
                foreach (var visionPlayer in player.GetSharedVisionList().Where(visionPlayer => visionPlayer.SeerView == player))
                    SendPacket(visionPlayer);

            if (player.SeerView == player || player.Vehicle != null)
                SendPacket(player);
        }
    }

    private void SendPacket(Player player)
    {
        // never send packet to self
        if (_source == player || (_team != 0 && player.EffectiveTeam != _team) || _skippedReceiver == player)
            return;

        if (!player.HaveAtClient(_source))
            return;

        _packetSender.Invoke(player);
    }
}