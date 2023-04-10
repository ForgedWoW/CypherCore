// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Events;

public class GameEventData
{
    public byte Announce { get; set; }
    public Dictionary<uint, GameEventFinishCondition> Conditions { get; set; } = new();
    public string Description { get; set; }
    public long End { get; set; }
    public HolidayIds HolidayID { get; set; }
    public byte HolidayStage { get; set; }

    public uint Length { get; set; }

    // occurs before this time
    public long Nextstart { get; set; }

    // after this time the follow-up events count this phase completed
    public uint Occurence { get; set; }

    // conditions to finish
    public List<ushort> PrerequisiteEvents { get; set; } = new();

    public long Start { get; set; } // occurs after this time

    // time between end and start
    // length of the event (Time.Minutes) after finishing all conditions
    public GameEventState State { get; set; } // state of the GameInfo event, these are saved into the game_event table on change!

    public GameEventData()
    {
        Start = 1;
    }

    // events that must be completed before starting this event
    // if 0 dont announce, if 1 announce, if 2 take config value
    public bool IsValid()
    {
        return Length > 0 || State > GameEventState.Normal;
    }
}