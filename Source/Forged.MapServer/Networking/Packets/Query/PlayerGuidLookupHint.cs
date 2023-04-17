// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Query;

public class PlayerGuidLookupHint
{
    public uint? NativeRealmAddress = new();

    public uint? VirtualRealmAddress = new(); // current realm (?) (identifier made from the Index, BattleGroup and Region)
    // original realm (?) (identifier made from the Index, BattleGroup and Region)

    public void Write(WorldPacket data)
    {
        data.WriteBit(VirtualRealmAddress.HasValue);
        data.WriteBit(NativeRealmAddress.HasValue);
        data.FlushBits();

        if (VirtualRealmAddress.HasValue)
            data.WriteUInt32(VirtualRealmAddress.Value);

        if (NativeRealmAddress.HasValue)
            data.WriteUInt32(NativeRealmAddress.Value);
    }
}