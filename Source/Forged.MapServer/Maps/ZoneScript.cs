// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Framework.Dynamic;

namespace Forged.MapServer.Maps;

public class ZoneScript
{
    protected EventMap Events = new();

    public virtual uint GetCreatureEntry(ulong guidlow, CreatureData data)
    {
        return data.Id;
    }

    //All-purpose data storage 32 bit
    public virtual uint GetData(uint dataId)
    {
        return 0;
    }

    public virtual ulong GetData64(uint dataId)
    {
        return 0;
    }

    public virtual uint GetGameObjectEntry(ulong spawnId, uint entry)
    {
        return entry;
    }

    //All-purpose data storage 64 bit
    public virtual ObjectGuid GetGuidData(uint dataId)
    {
        return ObjectGuid.Empty;
    }

    public virtual void OnCreatureCreate(Creature creature) { }

    public virtual void OnCreatureRemove(Creature creature) { }

    public virtual void OnGameObjectCreate(GameObject go) { }

    public virtual void OnGameObjectRemove(GameObject go) { }

    public virtual void OnUnitDeath(Unit unit) { }

    public virtual void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker) { }

    public virtual void SetData(uint dataId, uint value) { }

    public virtual void SetData64(uint dataId, ulong value) { }

    public virtual void SetGuidData(uint dataId, ObjectGuid value) { }

    public virtual void TriggerGameEvent(uint gameEventId, WorldObject source = null, WorldObject target = null)
    {
        if (source != null)
            GameEvents.Trigger(gameEventId, source, target);
        else
            ProcessEvent(null, gameEventId, null);
    }
}