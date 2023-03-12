// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ItemFlagsCustom
{
	Unused = 0x0001,
	IgnoreQuestStatus = 0x0002, // No quest status will be checked when this item drops
	FollowLootRules = 0x0004    // Item will always follow group/master/need before greed looting rules
}