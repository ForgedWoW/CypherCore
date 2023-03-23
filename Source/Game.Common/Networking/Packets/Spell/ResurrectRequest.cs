// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class ResurrectRequest : ServerPacket
{
	public ObjectGuid ResurrectOffererGUID;
	public uint ResurrectOffererVirtualRealmAddress;
	public uint PetNumber;
	public uint SpellID;
	public bool UseTimer;
	public bool Sickness;
	public string Name;
	public ResurrectRequest() : base(ServerOpcodes.ResurrectRequest) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ResurrectOffererGUID);
		_worldPacket.WriteUInt32(ResurrectOffererVirtualRealmAddress);
		_worldPacket.WriteUInt32(PetNumber);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBits(Name.GetByteCount(), 11);
		_worldPacket.WriteBit(UseTimer);
		_worldPacket.WriteBit(Sickness);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Name);
	}
}
