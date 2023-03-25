// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Server;

public struct ConnectToKey
{
	public ulong Raw
	{
		get { return ((ulong)AccountId | ((ulong)connectionType << 32) | (Key << 33)); }
		set
		{
			AccountId = (uint)(value & 0xFFFFFFFF);
			connectionType = (ConnectionType)((value >> 32) & 1);
			Key = (value >> 33);
		}
	}

	public uint AccountId;
	public ConnectionType connectionType;
	public ulong Key;
}