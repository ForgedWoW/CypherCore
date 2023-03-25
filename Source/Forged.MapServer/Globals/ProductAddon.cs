// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class ProductAddon
{
	public uint DisplayInfoEntry { get; set; }
	public byte DisableListing { get; set; }
	public byte DisableBuy { get; set; }
	public byte NameColorIndex { get; set; }
	public string ScriptName { get; set; } = "";
	public string Comment { get; set; } = "";
}