// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps;

public class SpawnGroupTemplateData
{
	public uint GroupId { get; set; }
	public string Name { get; set; }
	public uint MapId { get; set; }
	public SpawnGroupFlags Flags { get; set; }
}