// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossHelixGearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47297)]
public class NPCLumberingOafAI : ScriptedAI
{
    private readonly InstanceScript _instance;
    private readonly SummonList _summons;
    private readonly Vehicle _vehicle;

    public NPCLumberingOafAI(Creature pCreature) : base(pCreature)
    {
        _vehicle = Me.VehicleKit1;
        _instance = pCreature.InstanceScript;
        _summons = new SummonList(pCreature);
    }

    public override void Reset()
    {
        if (!Me || _vehicle == null)
            return;

        Events.Reset();
    }

    public override void JustEnteredCombat(Unit who)
    {
        if (!Me)
            return;

        Events.ScheduleEvent(HelOafEvents.EVENT_OAFQUARD, TimeSpan.FromMilliseconds(5000));
    }

    public override void JustDied(Unit killer)
    {
        var helix = Me.FindNearestCreature(DmCreatures.NPC_HELIX_GEARBREAKER, 200, true);

        if (helix != null)
        {
            var pAI = (BossHelixGearbreaker)helix.AI;

            if (pAI != null)
                pAI.OafDead();
        }
    }

    public void SummonBunny()
    {
        Talk(0);
        Talk(1);
        // _bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, OafPos[1].Location.X, OafPos[1].Location.Y, OafPos[1].Location.Z);
        Me.SetInCombatWithZone();
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (type != MovementGeneratorType.Point)
            return;

        if (id == 1)
        {
            //if (_bunny != null)
            //{
            //    me.SetInCombatWithZone();
            //    Unit passenger = me.GetVehicleKit().GetPassenger(1);
            //    if (passenger != null)
            //    {
            //        passenger.ExitVehicle();
            //        me.Attack(passenger, true);
            //    }

            //    if (_bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 100.0f))
            //    {
            //        me.SpellFactory.CastSpell(me, IsHeroic() ? eSpels.OAF_SMASH_H : eSpels.OAF_SMASH);

            //        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -303.193481f, -486.287140f, 49.185917f, 2.152038f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(360000));
            //        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -300.496674f, -490.433746f, 49.073387f, 5.243889f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
            //        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -298.689301f, -486.994995f, 48.893055f, 0.950859f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
            //        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -301.923676f, -486.674591f, 49.081684f, 2.677864f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));
            //        me.SummonCreature(DMCreatures.NPC_MINE_RAT, -296.066345f, -488.150177f, 48.917435f, 2.657793f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds( 360000));

            //        Unit.Kill(_bunny, _bunny);
            //        me.SetSpeed(UnitMoveType.Run, 1.0f);
            //    }
            //}

            var target = SelectTarget(SelectTargetMethod.MaxThreat);

            if (target != null)
                DoStartMovement(target);
        }
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (!UpdateVictim())
            return;

        if (!Me || _vehicle == null)
            return;

        Events.Update(uiDiff);

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
            switch (eventId)
            {
                case HelOafEvents.EVENT_OAFQUARD:
                    SummonBunny();
                    Events.ScheduleEvent(HelOafEvents.EVENT_MOUNT_PLAYER, TimeSpan.FromMilliseconds(500));

                    break;

                case HelOafEvents.EVENT_MOUNT_PLAYER:
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);

                    if (target != null)
                        target.SpellFactory.CastSpell(Me, ESpels.RIDE_VEHICLE_HARDCODED);

                    Events.ScheduleEvent(HelOafEvents.EVENT_MOVE_TO_POINT, TimeSpan.FromMilliseconds(500));

                    break;

                case HelOafEvents.EVENT_MOVE_TO_POINT:
                    Me.SetSpeed(UnitMoveType.Run, 5.0f);
                    Me.MotionMaster.MovePoint(0, -289.809f, -527.215f, 49.8021f);
                    Events.ScheduleEvent(HelOafEvents.EVEMT_CHARGE, TimeSpan.FromMilliseconds(2000));

                    break;

                case HelOafEvents.EVEMT_CHARGE:
                    //if (me.GetDistance(OafPos[0]) <= 2.0f)
                    //{
                    //    me.GetMotionMaster().Clear();
                    //    if (_bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 150.0f))
                    //    {
                    //        me.GetMotionMaster().MovePoint(1, _bunny.Location.X, _bunny.Location.Y, _bunny.Location.Z);
                    //        _bunny.SetUnitFlag(UnitFlags.Uninteractible);
                    //    }
                    //}
                    Events.ScheduleEvent(HelOafEvents.EVENT_FINISH, TimeSpan.FromMilliseconds(1500));

                    break;

                case HelOafEvents.EVENT_FINISH:
                    Events.ScheduleEvent(HelOafEvents.EVENT_OAFQUARD, TimeSpan.FromMilliseconds(17000));

                    break;
            }

        DoMeleeAttackIfReady();
    }
}