// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Framework.Dynamic;

namespace Forged.MapServer.Entities.Creatures;

public class AssistDelayEvent : BasicEvent
{
    private readonly List<ObjectGuid> _assistants = new();
    private readonly ObjectAccessor _objectAccessor;
    private readonly Unit _owner;


    private readonly ObjectGuid _victim;

    public AssistDelayEvent(ObjectGuid victim, Unit owner, ObjectAccessor objectAccessor)
    {
        _victim = victim;
        _owner = owner;
        _objectAccessor = objectAccessor;
    }

    public void AddAssistant(ObjectGuid guid)
    {
        _assistants.Add(guid);
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        var victim = _objectAccessor.GetUnit(_owner, _victim);

        if (victim == null)
            return true;

        while (!_assistants.Empty())
        {
            var assistant = _owner.Location.Map.GetCreature(_assistants[0]);
            _assistants.RemoveAt(0);

            if (assistant == null || !assistant.CanAssistTo(_owner, victim))
                continue;

            assistant.SetNoCallAssistance(true);
            assistant.EngageWithTarget(victim);
        }

        return true;
    }
}