// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public class InstanceLockData
{
    public uint CompletedEncountersMask { get; set; }
    public string Data { get; set; }
    public uint EntranceWorldSafeLocId { get; set; }
}