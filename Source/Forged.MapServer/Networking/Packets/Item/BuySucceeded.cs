// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class BuySucceeded : ServerPacket
{
	public ObjectGuid VendorGUID;
	public uint Muid;
	public uint QuantityBought;
	public uint NewQuantity;
	public BuySucceeded() : base(ServerOpcodes.BuySucceeded) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VendorGUID);
		_worldPacket.WriteUInt32(Muid);
		_worldPacket.WriteUInt32(NewQuantity);
		_worldPacket.WriteUInt32(QuantityBought);
	}
}