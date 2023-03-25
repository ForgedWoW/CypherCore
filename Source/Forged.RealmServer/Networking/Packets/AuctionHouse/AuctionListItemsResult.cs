// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class AuctionListItemsResult : ServerPacket
{
	public List<AuctionItem> Items = new();
	public uint Unknown830;
	public uint TotalCount;
	public uint DesiredDelay;
	public AuctionHouseListType ListType;
	public bool HasMoreResults;
	public AuctionBucketKey BucketKey = new();

	public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Items.Count);
		_worldPacket.WriteUInt32(Unknown830);
		_worldPacket.WriteUInt32(TotalCount);
		_worldPacket.WriteUInt32(DesiredDelay);
		_worldPacket.WriteBits((int)ListType, 2);
		_worldPacket.WriteBit(HasMoreResults);
		_worldPacket.FlushBits();

		BucketKey.Write(_worldPacket);

		foreach (var item in Items)
			item.Write(_worldPacket);
	}
}