// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49520)]
public class npc_lightning_orbs : NullCreatureAI
{
    private uint TurnTimer;

    public npc_lightning_orbs(Creature creature) : base(creature) { }

    public override void Reset()
    {
        TurnTimer = 100;
        var vehicle = Me.VehicleKit1;

        if (vehicle != null)
            for (sbyte i = 0; i < 8; i++)
                if (vehicle.HasEmptySeat(i))
                {
                    Creature pas = Me.SummonCreature(49521, Me.Location.X, Me.Location.Y, Me.Location.Z);

                    if (pas != null)
                        pas.EnterVehicle(Me, i);
                }
    }

    public override void UpdateAI(uint diff)
    {
        if (TurnTimer <= diff)
        {
            Me.SetFacingTo(Me.Location.Orientation + 0.05233f);
            TurnTimer = 100;
        }
        else
        {
            TurnTimer -= diff;
        }
    }
}