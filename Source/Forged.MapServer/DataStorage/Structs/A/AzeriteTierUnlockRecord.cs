// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AzeriteTierUnlockRecord
{
    public uint Id;
    public byte ItemCreationContext;
    public byte Tier;
    public byte AzeriteLevel;
    public uint AzeriteTierUnlockSetID;
}