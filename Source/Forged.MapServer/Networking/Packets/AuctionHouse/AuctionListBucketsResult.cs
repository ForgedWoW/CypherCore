﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class AuctionListBucketsResult : ServerPacket
{
	public List<BucketInfo> Buckets = new();
	public uint DesiredDelay;
	public int Unknown830_0;
	public int Unknown830_1;
	public AuctionHouseBrowseMode BrowseMode;
	public bool HasMoreResults;

	public AuctionListBucketsResult() : base(ServerOpcodes.AuctionListBucketsResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Buckets.Count);
		_worldPacket.WriteUInt32(DesiredDelay);
		_worldPacket.WriteInt32(Unknown830_0);
		_worldPacket.WriteInt32(Unknown830_1);
		_worldPacket.WriteBits((int)BrowseMode, 1);
		_worldPacket.WriteBit(HasMoreResults);
		_worldPacket.FlushBits();

		foreach (var bucketInfo in Buckets)
			bucketInfo.Write(_worldPacket);
	}
}