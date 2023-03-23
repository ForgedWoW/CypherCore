using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.B;

public sealed class BannedAddonsRecord
{
	public uint Id;
	public string Name;
	public string Version;
	public byte Flags;
}
