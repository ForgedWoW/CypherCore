// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal struct PartyLootSettings
{
    public ObjectGuid LootMaster;

    public byte Method;

    public byte Threshold;

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(Method);
        data.WritePackedGuid(LootMaster);
        data.WriteUInt8(Threshold);
    }
}