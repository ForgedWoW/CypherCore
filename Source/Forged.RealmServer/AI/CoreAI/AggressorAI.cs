// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities.Creatures;

namespace Forged.RealmServer.AI;

public class AggressorAI : CreatureAI
{
	public AggressorAI(Creature c) : base(c) { }

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}
}