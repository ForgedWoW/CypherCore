// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Query;

namespace Game.Common.Networking.Packets.Query;

public class QueryGameObjectResponse : ServerPacket
{
	public uint GameObjectID;
	public ObjectGuid Guid;
	public bool Allow;
	public GameObjectStats Stats;
	public QueryGameObjectResponse() : base(ServerOpcodes.QueryGameObjectResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(GameObjectID);
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(Allow);
		_worldPacket.FlushBits();

		ByteBuffer statsData = new();

		if (Allow)
		{
			statsData.WriteUInt32(Stats.Type);
			statsData.WriteUInt32(Stats.DisplayID);

			for (var i = 0; i < 4; i++)
				statsData.WriteCString(Stats.Name[i]);

			statsData.WriteCString(Stats.IconName);
			statsData.WriteCString(Stats.CastBarCaption);
			statsData.WriteCString(Stats.UnkString);

			for (uint i = 0; i < SharedConst.MaxGOData; i++)
				statsData.WriteInt32(Stats.Data[i]);

			statsData.WriteFloat(Stats.Size);
			statsData.WriteUInt8((byte)Stats.QuestItems.Count);

			foreach (var questItem in Stats.QuestItems)
				statsData.WriteUInt32(questItem);

			statsData.WriteUInt32(Stats.ContentTuningId);
		}

		_worldPacket.WriteUInt32(statsData.GetSize());

		if (statsData.GetSize() != 0)
			_worldPacket.WriteBytes(statsData);
	}
}
