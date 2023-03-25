// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities.Creatures;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities.Units;

namespace Forged.RealmServer.AI;

public class ReactorAI : CreatureAI
{
	public ReactorAI(Creature c) : base(c) { }

	public override void MoveInLineOfSight(Unit who) { }

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}
}