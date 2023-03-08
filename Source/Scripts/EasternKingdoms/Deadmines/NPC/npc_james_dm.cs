// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49539)]
public class npc_james_dm : ScriptedAI
{
	public npc_james_dm(Creature creature) : base(creature) { }

	public override void JustDied(Unit killer)
	{
		var Vanessa = me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

		if (Vanessa != null)
		{
			var pAI = (npc_vanessa_nightmare)Vanessa.GetAI();

			if (pAI != null)
				pAI.WorgenKilled();
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!me.GetVehicleKit())
			return;

		var Calissa = me.GetVehicleKit().GetPassenger(0);

		if (Calissa != null)
		{
			Calissa.SetInCombatWith(me, true);
			Calissa.GetThreatManager().AddThreat(me, 100000.0f);
			DoZoneInCombat();
		}
	}
}