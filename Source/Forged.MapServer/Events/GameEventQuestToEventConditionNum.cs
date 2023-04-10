// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Events;

public class GameEventQuestToEventConditionNum
{
    public uint Condition { get; set; }
    public ushort EventID { get; set; }
    public float Num { get; set; }
}