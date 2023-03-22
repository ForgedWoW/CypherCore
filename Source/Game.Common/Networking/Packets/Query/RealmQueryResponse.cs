// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class RealmQueryResponse : ServerPacket
{
	public uint VirtualRealmAddress;
	public byte LookupState;
	public VirtualRealmNameInfo NameInfo;
	public RealmQueryResponse() : base(ServerOpcodes.RealmQueryResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(VirtualRealmAddress);
		_worldPacket.WriteUInt8(LookupState);

		if (LookupState == 0)
			NameInfo.Write(_worldPacket);
	}
}