// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Events;

public class GameEventFinishCondition
{
    public float Done { get; set; }

    public uint DoneWorldState { get; set; }

    // done number
    public uint MaxWorldState { get; set; }

    public float ReqNum { get; set; } // required number // use float, since some events use percent
    // max resource count world state update id
    // done resource count world state update id
}