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
		var vehicle = me.VehicleKit1;

		if (vehicle != null)
			for (sbyte i = 0; i < 8; i++)
				if (vehicle.HasEmptySeat(i))
				{
					Creature pas = me.SummonCreature(49521, me.Location.X, me.Location.Y, me.Location.Z);

					if (pas != null)
						pas.EnterVehicle(me, i);
				}
	}

	public override void UpdateAI(uint diff)
	{
		if (TurnTimer <= diff)
		{
			me.SetFacingTo(me.Location.Orientation + 0.05233f);
			TurnTimer = 100;
		}
		else
		{
			TurnTimer -= diff;
		}
	}
}