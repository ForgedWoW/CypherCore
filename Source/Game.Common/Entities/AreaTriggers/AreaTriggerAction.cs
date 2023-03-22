﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public struct AreaTriggerAction
{
	public uint Param;
	public AreaTriggerActionTypes ActionType;
	public AreaTriggerActionUserTypes TargetType;
}