// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
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
    private readonly float _distSq;
    private readonly T _packetSender;
    private readonly PhaseShift _phaseShift;
    private readonly Unit _source;
    public MessageDistDelivererToHostile(Unit src, T packetSender, float dist, GridType gridType)
    {
        _source = src;
        _packetSender = packetSender;
        _phaseShift = src.Location.PhaseShift;
        _distSq = dist * dist;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (creature.Location.GetExactDist2dSq(_source.Location) > _distSq)
                continue;

            // Send packet to all who are sharing the creature's vision
            if (!creature.HasSharedVision)
                continue;

            foreach (var player in creature.GetSharedVisionList().Where(player => player.SeerView == creature))
                SendPacket(player);
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        foreach (var dynamicObject in objs)
        {
            if (!dynamicObject.Location.InSamePhase(_phaseShift))
                continue;

            if (dynamicObject.Location.GetExactDist2dSq(_source.Location) > _distSq)
                continue;

            var player = dynamicObject.GetCaster()?.AsPlayer;

            if (player == null)
                continue;

            // Send packet back to the caster if the caster has vision of dynamic object
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

            if (player.Location.GetExactDist2dSq(_source.Location) > _distSq)
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
        if (player == _source || !player.HaveAtClient(_source) || player.WorldObjectCombat.IsFriendlyTo(_source))
            return;

        _packetSender.Invoke(player);
    }
}