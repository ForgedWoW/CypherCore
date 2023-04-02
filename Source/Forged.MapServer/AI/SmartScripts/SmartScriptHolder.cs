// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

public class SmartScriptHolder : IComparable<SmartScriptHolder>
{
    public const uint DefaultPriority = uint.MaxValue;

    public SmartAction Action;
    public bool Active;
    public bool EnableTimed;
    public int EntryOrGuid;
    public SmartEvent Event;
    public uint EventId;
    public uint Link;
    public uint Priority;
    public bool RunOnce;
    public SmartScriptType SourceType;
    public SmartTarget Target;
    public uint Timer;
    public SmartScriptHolder() { }

    public SmartScriptHolder(SmartScriptHolder other)
    {
        EntryOrGuid = other.EntryOrGuid;
        SourceType = other.SourceType;
        EventId = other.EventId;
        Link = other.Link;
        Event = other.Event;
        Action = other.Action;
        Target = other.Target;
        Timer = other.Timer;
        Active = other.Active;
        RunOnce = other.RunOnce;
        EnableTimed = other.EnableTimed;
    }

    public int CompareTo(SmartScriptHolder other)
    {
        var result = Priority.CompareTo(other.Priority);

        if (result == 0)
            result = EntryOrGuid.CompareTo(other.EntryOrGuid);

        if (result == 0)
            result = SourceType.CompareTo(other.SourceType);

        if (result == 0)
            result = EventId.CompareTo(other.EventId);

        if (result == 0)
            result = Link.CompareTo(other.Link);

        return result;
    }

    public SmartActions GetActionType()
    {
        return Action.type;
    }

    public SmartEvents GetEventType()
    {
        return Event.type;
    }

    public SmartScriptType GetScriptType()
    {
        return SourceType;
    }
    public SmartTargets GetTargetType()
    {
        return Target.type;
    }

    public override string ToString()
    {
        return $"Entry {EntryOrGuid} SourceType {GetScriptType()} Event {EventId} Action {GetActionType()}";
    }
}