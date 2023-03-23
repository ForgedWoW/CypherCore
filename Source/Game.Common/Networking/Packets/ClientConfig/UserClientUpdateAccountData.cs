// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.ClientConfig;

public class UserClientUpdateAccountData : ClientPacket
{
	public ObjectGuid PlayerGuid;
	public long Time; // UnixTime
	public uint Size; // decompressed size
	public AccountDataTypes DataType = 0;
	public ByteBuffer CompressedData;
	public UserClientUpdateAccountData(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PlayerGuid = _worldPacket.ReadPackedGuid();
		Time = _worldPacket.ReadInt64();
		Size = _worldPacket.ReadUInt32();
		DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);

		var compressedSize = _worldPacket.ReadUInt32();

		if (compressedSize != 0)
			CompressedData = new ByteBuffer(_worldPacket.ReadBytes(compressedSize));
	}
}
