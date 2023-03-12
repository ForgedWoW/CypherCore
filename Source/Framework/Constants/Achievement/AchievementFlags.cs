// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum AchievementFlags
{
	Counter = 0x01,
	Hidden = 0x02,
	PlayNoVisual = 0x04,
	Summ = 0x08,
	MaxUsed = 0x10,
	ReqCount = 0x20,
	Average = 0x40,
	Bar = 0x80,
	RealmFirstReach = 0x100,
	RealmFirstKill = 0x200,
	Unk3 = 0x400,
	HideIncomplete = 0x800,
	ShowInGuildNews = 0x1000,
	ShowInGuildHeader = 0x2000,
	Guild = 0x4000,
	ShowGuildMembers = 0x8000,
	ShowCriteriaMembers = 0x10000,
	Account = 0x20000,
	Unk5 = 0x00040000,
	HideZeroCounter = 0x00080000,
	TrackingFlag = 0x00100000
}