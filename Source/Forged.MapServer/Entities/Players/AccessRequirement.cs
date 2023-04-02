// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Players;

public class AccessRequirement
{
    public uint Achievement { get; set; }
    public uint Item { get; set; }
    public uint Item2 { get; set; }
    public byte LevelMax { get; set; }
    public byte LevelMin { get; set; }
    public uint QuestA { get; set; }
    public string QuestFailedText { get; set; }
    public uint QuestH { get; set; }
}