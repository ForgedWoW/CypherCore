// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.ClientConfig;

public class UpdateAccountData : ServerPacket
{
	public ObjectGuid Player;
	public long Time; // UnixTime
	public uint Size; // decompressed size
	public AccountDataTypes DataType = 0;
	public ByteBuffer CompressedData;
	public UpdateAccountData() : base(ServerOpcodes.UpdateAccountData) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WriteInt64(Time);
		_worldPacket.WriteUInt32(Size);
		_worldPacket.WriteBits(DataType, 4);

		if (CompressedData == null)
		{
			_worldPacket.WriteUInt32(0);
		}
		else
		{
			var bytes = CompressedData.GetData();
			_worldPacket.WriteInt32(bytes.Length);
			_worldPacket.WriteBytes(bytes);
		}
	}
}
