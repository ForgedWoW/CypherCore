// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;

namespace Game.AI;

public class SummonList : List<ObjectGuid>
{
	readonly Creature _me;

	public SummonList(Creature creature)
	{
		_me = creature;
	}

	public void Summon(Creature summon)
	{
		Add(summon.GUID);
	}

	public void DoZoneInCombat(uint entry = 0)
	{
		foreach (var id in this)
		{
			var summon = ObjectAccessor.GetCreature(_me, id);

			if (summon && summon.IsAIEnabled && (entry == 0 || summon.Entry == entry))
				summon.AI.DoZoneInCombat(null);
		}
	}

	public void DespawnEntry(uint entry)
	{
		foreach (var id in this)
		{
			var summon = ObjectAccessor.GetCreature(_me, id);

			if (!summon)
			{
				Remove(id);
			}
			else if (summon.Entry == entry)
			{
				Remove(id);
				summon.DespawnOrUnsummon();
			}
		}
	}

	public void DespawnAll()
	{
		while (!this.Empty())
		{
			var summon = ObjectAccessor.GetCreature(_me, this.FirstOrDefault());
			RemoveAt(0);

			if (summon)
				summon.DespawnOrUnsummon();
		}
	}

	public void Despawn(Creature summon)
	{
		Remove(summon.GUID);
	}

	public void DespawnIf(ICheck<ObjectGuid> predicate)
	{
		this.RemoveAll(predicate);
	}

	public void DespawnIf(Predicate<ObjectGuid> predicate)
	{
		RemoveAll(predicate);
	}

	public void RemoveNotExisting()
	{
		foreach (var id in this)
			if (!ObjectAccessor.GetCreature(_me, id))
				Remove(id);
	}

	public void DoAction(int info, ICheck<ObjectGuid> predicate, ushort max = 0)
	{
		// We need to use a copy of SummonList here, otherwise original SummonList would be modified
		List<ObjectGuid> listCopy = new(this);
		listCopy.RandomResize(predicate.Invoke, max);
		DoActionImpl(info, listCopy);
	}

	public void DoAction(int info, Predicate<ObjectGuid> predicate, ushort max = 0)
	{
		// We need to use a copy of SummonList here, otherwise original SummonList would be modified
		List<ObjectGuid> listCopy = new(this);
		listCopy.RandomResize(predicate, max);
		DoActionImpl(info, listCopy);
	}

	public bool HasEntry(uint entry)
	{
		foreach (var id in this)
		{
			var summon = ObjectAccessor.GetCreature(_me, id);

			if (summon && summon.Entry == entry)
				return true;
		}

		return false;
	}

	void DoActionImpl(int action, List<ObjectGuid> summons)
	{
		foreach (var guid in summons)
		{
			var summon = ObjectAccessor.GetCreature(_me, guid);

			if (summon && summon.IsAIEnabled)
				summon.AI.DoAction(action);
		}
	}
}