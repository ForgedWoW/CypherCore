// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class VendorInventory : ServerPacket
{
	public byte Reason = 0;
	public List<VendorItemPkt> Items = new();
	public ObjectGuid Vendor;
	public VendorInventory() : base(ServerOpcodes.VendorInventory, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Vendor);
		_worldPacket.WriteUInt8(Reason);
		_worldPacket.WriteInt32(Items.Count);

		foreach (var item in Items)
			item.Write(_worldPacket);
	}
}