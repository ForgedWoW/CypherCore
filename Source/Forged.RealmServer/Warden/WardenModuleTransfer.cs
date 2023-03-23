// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.IO;
using Game.Common.Networking.Packets.Warden;

namespace Forged.RealmServer;

class WardenModuleTransfer
{
	public WardenOpcodes Command;
	public ushort DataSize;
	public byte[] Data = new byte[500];

	public static implicit operator byte[](WardenModuleTransfer transfer)
	{
		var buffer = new ByteBuffer();
		buffer.WriteUInt8((byte)transfer.Command);
		buffer.WriteUInt16(transfer.DataSize);
		buffer.WriteBytes(transfer.Data, 500);

		return buffer.GetData();
	}
}