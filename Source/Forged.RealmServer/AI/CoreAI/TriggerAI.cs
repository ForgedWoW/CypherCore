// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.AI;

public class TriggerAI : NullCreatureAI
{
	public TriggerAI(Creature c) : base(c) { }

	public override void IsSummonedBy(WorldObject summoner)
	{
		if (Me.Spells[0] != 0)
		{
			CastSpellExtraArgs extra = new();
			extra.OriginalCaster = summoner.GUID;
			Me.CastSpell(Me, Me.Spells[0], extra);
		}
	}
}