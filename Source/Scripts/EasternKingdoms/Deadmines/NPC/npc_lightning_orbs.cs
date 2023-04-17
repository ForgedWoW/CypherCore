// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49520)]
public class NPCLightningOrbs : NullCreatureAI
{
    private uint _turnTimer;

    public NPCLightningOrbs(Creature creature) : base(creature) { }

    public override void Reset()
    {
        _turnTimer = 100;
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
        if (_turnTimer <= diff)
        {
            Me.SetFacingTo(Me.Location.Orientation + 0.05233f);
            _turnTimer = 100;
        }
        else
        {
            _turnTimer -= diff;
        }
    }
}