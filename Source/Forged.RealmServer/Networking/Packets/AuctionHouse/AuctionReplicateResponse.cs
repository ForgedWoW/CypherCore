// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class AuctionReplicateResponse : ServerPacket
{
	public uint ChangeNumberCursor;
	public uint ChangeNumberGlobal;
	public uint DesiredDelay;
	public uint ChangeNumberTombstone;
	public uint Result;
	public List<AuctionItem> Items = new();

	public AuctionReplicateResponse() : base(ServerOpcodes.AuctionReplicateResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Result);
		_worldPacket.WriteUInt32(DesiredDelay);
		_worldPacket.WriteUInt32(ChangeNumberGlobal);
		_worldPacket.WriteUInt32(ChangeNumberCursor);
		_worldPacket.WriteUInt32(ChangeNumberTombstone);
		_worldPacket.WriteInt32(Items.Count);

		foreach (var item in Items)
			item.Write(_worldPacket);
	}
}