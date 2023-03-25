// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SetMeleeAnimKit : ServerPacket
{
	public ObjectGuid Unit;
	public ushort AnimKitID;
	public SetMeleeAnimKit() : base(ServerOpcodes.SetMeleeAnimKit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt16(AnimKitID);
	}
}