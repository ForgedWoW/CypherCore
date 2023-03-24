// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.BattlePet;

public class BattlePetSlot
{
	public BattlePetStruct Pet;
	public uint CollarID;
	public byte Index;
	public bool Locked = true;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Pet.Guid.IsEmpty ? ObjectGuid.Create(HighGuid.BattlePet, 0) : Pet.Guid);
		data.WriteUInt32(CollarID);
		data.WriteUInt8(Index);
		data.WriteBit(Locked);
		data.FlushBits();
	}
}
