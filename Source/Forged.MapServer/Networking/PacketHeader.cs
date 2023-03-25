// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.IO;

namespace Forged.MapServer.Networking;

public class PacketHeader
{
	public int Size;
	public byte[] Tag = new byte[12];

	public void Read(byte[] buffer)
	{
		Size = BitConverter.ToInt32(buffer, 0);
		Buffer.BlockCopy(buffer, 4, Tag, 0, 12);
	}

	public void Write(ByteBuffer byteBuffer)
	{
		byteBuffer.WriteInt32(Size);
		byteBuffer.WriteBytes(Tag, 12);
	}

	public bool IsValidSize()
	{
		return Size < 0x40000;
	}
}