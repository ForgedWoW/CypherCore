using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class MessageDistDeliverer<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
{
    public GridType GridType { get; set; } = GridType.World;

    readonly WorldObject i_source;
    readonly T i_packetSender;
    readonly PhaseShift i_phaseShift;
    readonly float i_distSq;
    readonly Team team;
    readonly Player skipped_receiver;
    readonly bool required3dDist;

    public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
    {
        i_source = src;
        i_packetSender = packetSender;
        i_phaseShift = src.GetPhaseShift();
        i_distSq = dist * dist;
        if (own_team_only && src.IsPlayer())
            team = src.ToPlayer().GetEffectiveTeam();

        skipped_receiver = skipped;
        required3dDist = req3dDist;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if ((!required3dDist ? player.GetExactDist2dSq(i_source) : player.GetExactDistSq(i_source)) > i_distSq)
                continue;

            // Send packet to all who are sharing the player's vision
            if (player.HasSharedVision())
            {
                foreach (var visionPlayer in player.GetSharedVisionList())
                    if (visionPlayer.seerView == player)
                        SendPacket(visionPlayer);
            }

            if (player.seerView == player || player.GetVehicle() != null)
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

            if ((!required3dDist ? creature.GetExactDist2dSq(i_source) : creature.GetExactDistSq(i_source)) > i_distSq)
                continue;

            // Send packet to all who are sharing the creature's vision
            if (creature.HasSharedVision())
            {
                foreach (var visionPlayer in creature.GetSharedVisionList())
                    if (visionPlayer.seerView == creature)
                        SendPacket(visionPlayer);
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

            if ((!required3dDist ? dynamicObject.GetExactDist2dSq(i_source) : dynamicObject.GetExactDistSq(i_source)) > i_distSq)
                continue;

            // Send packet back to the caster if the caster has vision of dynamic object
            Unit caster = dynamicObject.GetCaster();
            if (caster)
            {
                Player player = caster.ToPlayer();
                if (player && player.seerView == dynamicObject)
                    SendPacket(player);
            }
        }
    }

    void SendPacket(Player player)
    {
        // never send packet to self
        if (i_source == player || (team != 0 && player.GetEffectiveTeam() != team) || skipped_receiver == player)
            return;

        if (!player.HaveAtClient(i_source))
            return;

        i_packetSender.Invoke(player);
    }
}