// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class MissileCancel : ServerPacket
{
	public ObjectGuid OwnerGUID;
	public bool Reverse;
	public uint SpellID;
	public MissileCancel() : base(ServerOpcodes.MissileCancel) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(OwnerGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(Reverse);
		_worldPacket.FlushBits();
	}
}
