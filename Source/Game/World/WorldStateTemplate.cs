// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game;

public class WorldStateTemplate
{
	public int Id;
	public int DefaultValue;
	public uint ScriptId;

	public List<int> MapIds = new();
	public List<uint> AreaIds = new();
}