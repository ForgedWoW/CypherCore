// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Globals;

namespace Game.Maps;

public struct ScriptAction
{
	public ObjectGuid OwnerGUID;

	// owner of source if source is item
	public ScriptInfo Script;

	public ObjectGuid SourceGUID;
	public ObjectGuid TargetGUID;
}