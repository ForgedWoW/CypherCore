// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49550)]
public class NPCRopeAway : ScriptedAI
{
    private bool _runAway;
    private byte _phase;
    private uint _moveTimer;

    public NPCRopeAway(Creature creature) : base(creature)
    {
        Me.SetNpcFlag(NPCFlags.SpellClick);
    }

    public override void Reset()
    {
        _phase = 0;
        _moveTimer = 500;
        _runAway = false;
        Me.SetSpeed(UnitMoveType.Flight, 3.0f);
    }

    public override void MovementInform(MovementGeneratorType unnamedParameter, uint id)
    {
        if (id == 1)
        {
            var passenger = Me.VehicleKit1.GetPassenger(0);

            if (passenger != null)
                passenger.ExitVehicle();
        }
    }

    public override void PassengerBoarded(Unit who, sbyte unnamedParameter, bool apply)
    {
        if (who.TypeId == TypeId.Player)
            if (apply)
                _runAway = true;
    }

    public override void UpdateAI(uint diff)
    {
        if (_runAway)
        {
            if (_moveTimer <= diff)
                switch (_phase)
                {
                    case 0:
                        Me.MotionMaster.MovePoint(0, -77.97f, -877.09f, 49.44f);
                        _moveTimer = 2500;
                        _phase++;

                        break;
                    case 1:
                        Me.MotionMaster.MovePoint(1, -64.02f, -839.84f, 41.22f);
                        _moveTimer = 3000;
                        _phase++;

                        break;
                    case 2:
                        break;
                }
            else
                _moveTimer -= diff;
        }
    }
}