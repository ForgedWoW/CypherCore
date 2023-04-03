using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Events;

public class GameEventData
{
    public byte Announce;
    public Dictionary<uint, GameEventFinishCondition> Conditions = new();
    public string Description;
    public long End;
    public HolidayIds HolidayID;
    public byte HolidayStage;
    public uint Length;
    // occurs before this time
    public long Nextstart;

    // after this time the follow-up events count this phase completed
    public uint Occurence;

    // conditions to finish
    public List<ushort> PrerequisiteEvents = new();

    public long Start; // occurs after this time
    // time between end and start
    // length of the event (Time.Minutes) after finishing all conditions
    public GameEventState State; // state of the GameInfo event, these are saved into the game_event table on change!
    // events that must be completed before starting this event
    // if 0 dont announce, if 1 announce, if 2 take config value

    public GameEventData()
    {
        Start = 1;
    }

    public bool IsValid()
    {
        return Length > 0 || State > GameEventState.Normal;
    }
}