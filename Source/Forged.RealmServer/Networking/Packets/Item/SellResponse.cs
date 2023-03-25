// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class SellResponse : ServerPacket
{
	public ObjectGuid VendorGUID;
	public ObjectGuid ItemGUID;
	public SellResult Reason = SellResult.Unk;
	public SellResponse() : base(ServerOpcodes.SellResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VendorGUID);
		_worldPacket.WritePackedGuid(ItemGUID);
		_worldPacket.WriteUInt8((byte)Reason);
	}
}