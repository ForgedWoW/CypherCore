// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public struct AuraInfo
{
    public AuraDataInfo AuraData;

    public byte Slot;

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(Slot);
        data.WriteBit(AuraData != null);
        data.FlushBits();

        AuraData?.Write(data);
    }
}