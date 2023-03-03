using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class MessageDistDelivererToHostile<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
    public GridType GridType { get; set; }

    readonly Unit i_source;
    readonly T i_packetSender;
    readonly PhaseShift i_phaseShift;
    readonly float i_distSq;

    public MessageDistDelivererToHostile(Unit src, T packetSender, float dist, GridType gridType)
    {
        i_source = src;
        i_packetSender = packetSender;
        i_phaseShift = src.GetPhaseShift();
        i_distSq = dist * dist;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if (player.GetExactDist2dSq(i_source) > i_distSq)
                continue;

            // Send packet to all who are sharing the player's vision
            if (player.HasSharedVision())
            {
                foreach (var visionPlayer in player.GetSharedVisionList())
                    if (visionPlayer.seerView == player)
                        SendPacket(visionPlayer);
            }

            if (player.seerView == player || player.GetVehicle())
                SendPacket(player);
        }
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.InSamePhase(i_phaseShift))
                continue;

            if (creature.GetExactDist2dSq(i_source) > i_distSq)
                continue;

            // Send packet to all who are sharing the creature's vision
            if (creature.HasSharedVision())
            {
                foreach (var player in creature.GetSharedVisionList())
                    if (player.seerView == creature)
                        SendPacket(player);
            }
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            DynamicObject dynamicObject = objs[i];
            if (!dynamicObject.InSamePhase(i_phaseShift))
                continue;

            if (dynamicObject.GetExactDist2dSq(i_source) > i_distSq)
                continue;

            Unit caster = dynamicObject.GetCaster();
            if (caster != null)
            {
                // Send packet back to the caster if the caster has vision of dynamic object
                Player player = caster.ToPlayer();
                if (player && player.seerView == dynamicObject)
                    SendPacket(player);
            }
        }
    }

    void SendPacket(Player player)
    {
        // never send packet to self
        if (player == i_source || !player.HaveAtClient(i_source) || player.IsFriendlyTo(i_source))
            return;

        i_packetSender.Invoke(player);
    }
}