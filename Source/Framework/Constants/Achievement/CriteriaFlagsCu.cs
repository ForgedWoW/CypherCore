// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CriteriaFlagsCu
{
	Player = 0x1,
	Account = 0x2,
	Guild = 0x4,
	Scenario = 0x8,
	QuestObjective = 0x10
}