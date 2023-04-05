// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Creatures;

public class CreatureLevelScaling
{
    public uint ContentTuningId { get; set; }
    public short DeltaLevelMax { get; set; }
    public short DeltaLevelMin { get; set; }
}