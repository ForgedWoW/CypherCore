// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ReportMinorCategory
{
	TextChat = 0x0001,
	Boosting = 0x0002,
	Spam = 0x0004,
	Afk = 0x0008,
	IntentionallyFeeding = 0x0010,
	BlockingProgress = 0x0020,
	Hacking = 0x0040,
	Botting = 0x0080,
	Advertisement = 0x0100,
	BTag = 0x0200,
	GroupName = 0x0400,
	CharacterName = 0x0800,
	GuildName = 0x1000,
	Description = 0x2000,
	Name = 0x4000,
}