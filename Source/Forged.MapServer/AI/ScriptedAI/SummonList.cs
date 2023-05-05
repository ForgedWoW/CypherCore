// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;

namespace Forged.MapServer.AI.ScriptedAI;

public class SummonList : List<ObjectGuid>
{
    private readonly Creature _me;

    public SummonList(Creature creature)
    {
        _me = creature;
    }

    public void Despawn(Creature summon)
    {
        Remove(summon.GUID);
    }

    public void DespawnAll()
    {
        while (!this.Empty())
        {
            var summon = ObjectAccessor.GetCreature(_me, this.FirstOrDefault());
            RemoveAt(0);
            summon?.DespawnOrUnsummon();
        }
    }

    public void DespawnEntry(uint entry)
    {
        foreach (var id in this)
        {
            var summon = ObjectAccessor.GetCreature(_me, id);

            if (summon == null)
                Remove(id);
            else if (summon.Entry == entry)
            {
                Remove(id);
                summon.DespawnOrUnsummon();
            }
        }
    }

    public void DespawnIf(ICheck<ObjectGuid> predicate)
    {
        this.RemoveAll(predicate);
    }

    public void DespawnIf(Predicate<ObjectGuid> predicate)
    {
        RemoveAll(predicate);
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

    public void DoZoneInCombat(uint entry = 0)
    {
        foreach (var id in this)
        {
            var summon = ObjectAccessor.GetCreature(_me, id);

            if (summon is { IsAIEnabled: true } && (entry == 0 || summon.Entry == entry))
                summon.AI.DoZoneInCombat();
        }
    }

    public bool HasEntry(uint entry)
    {
        foreach (var id in this)
        {
            var summon = ObjectAccessor.GetCreature(_me, id);

            if (summon != null && summon.Entry == entry)
                return true;
        }

        return false;
    }

    public void RemoveNotExisting()
    {
        foreach (ObjectGuid id in this.Where(id => ObjectAccessor.GetCreature(_me, id) == null))
            Remove(id);
    }

    public void Summon(Creature summon)
    {
        Add(summon.GUID);
    }

    private void DoActionImpl(int action, List<ObjectGuid> summons)
    {
        foreach (var summon in summons.Select(guid => ObjectAccessor.GetCreature(_me, guid)).Where(summon => summon is { IsAIEnabled: true }))
        {
            summon.AI.DoAction(action);
        }
    }
}