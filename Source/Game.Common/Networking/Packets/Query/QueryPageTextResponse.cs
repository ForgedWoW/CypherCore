// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Query;

namespace Game.Common.Networking.Packets.Query;

public class QueryPageTextResponse : ServerPacket
{
	public uint PageTextID;
	public bool Allow;
	public List<PageTextInfo> Pages = new();
	public QueryPageTextResponse() : base(ServerOpcodes.QueryPageTextResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(PageTextID);
		_worldPacket.WriteBit(Allow);
		_worldPacket.FlushBits();

		if (Allow)
		{
			_worldPacket.WriteInt32(Pages.Count);

			foreach (var pageText in Pages)
				pageText.Write(_worldPacket);
		}
	}

	public struct PageTextInfo
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt32(Id);
			data.WriteUInt32(NextPageID);
			data.WriteInt32(PlayerConditionID);
			data.WriteUInt8(Flags);
			data.WriteBits(Text.GetByteCount(), 12);
			data.FlushBits();

			data.WriteString(Text);
		}

		public uint Id;
		public uint NextPageID;
		public int PlayerConditionID;
		public byte Flags;
		public string Text;
	}
}
