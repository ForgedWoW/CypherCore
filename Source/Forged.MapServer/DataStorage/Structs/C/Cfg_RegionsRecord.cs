// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class Cfg_RegionsRecord
{
    public uint ChallengeOrigin;
    public uint Id;

    public uint Raidorigin;

    // Date of first raid reset, all other resets are calculated as this date plus interval
    public byte RegionGroupMask;

    public ushort RegionID;
    public string Tag;
}