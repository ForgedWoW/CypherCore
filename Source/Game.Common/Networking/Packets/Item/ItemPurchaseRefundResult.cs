// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Item;

public class ItemPurchaseRefundResult : ServerPacket
{
	public byte Result;
	public ObjectGuid ItemGUID;
	public ItemPurchaseContents Contents;
	public ItemPurchaseRefundResult() : base(ServerOpcodes.ItemPurchaseRefundResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGUID);
		_worldPacket.WriteUInt8(Result);
		_worldPacket.WriteBit(Contents != null);
		_worldPacket.FlushBits();

		if (Contents != null)
			Contents.Write(_worldPacket);
	}
}
