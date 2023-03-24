// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Warden;

public class ClientWardenModule
{
	public byte[] Id = new byte[16];
	public byte[] Key = new byte[16];
	public byte[] CompressedData;
	public uint CompressedSize;
}
