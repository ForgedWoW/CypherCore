// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

public class BattlePetSlot
{
    public uint CollarID;
    public byte Index;
    public bool Locked = true;
    public BattlePetStruct Pet;
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(Pet.Guid.IsEmpty ? ObjectGuid.Create(HighGuid.BattlePet, 0) : Pet.Guid);
        data.WriteUInt32(CollarID);
        data.WriteUInt8(Index);
        data.WriteBit(Locked);
        data.FlushBits();
    }
}