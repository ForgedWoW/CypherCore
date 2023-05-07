// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactUnlockRecord
{
    public uint ArtifactID;
    public uint Id;
    public ushort ItemBonusListID;
    public uint PlayerConditionID;
    public uint PowerID;
    public byte PowerRank;
}