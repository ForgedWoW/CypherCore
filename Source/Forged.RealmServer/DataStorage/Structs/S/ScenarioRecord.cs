// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class ScenarioRecord
{
	public uint Id;
	public string Name;
	public ushort AreaTableID;
	public byte Type;
	public byte Flags;
	public uint UiTextureKitID;
}