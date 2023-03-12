// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AccountDataTypes
{
	GlobalConfigCache = 0x00,
	PerCharacterConfigCache = 0x01,
	GlobalBindingsCache = 0x02,
	PerCharacterBindingsCache = 0x03,
	GlobalMacrosCache = 0x04,
	PerCharacterMacrosCache = 0x05,
	PerCharacterLayoutCache = 0x06,
	PerCharacterChatCache = 0x07,
	GlobalTtsCache = 8,
	PerCharacterTtsCache = 9,
	GlobalFlaggedCache = 10,
	PerCharacterFlaggedCache = 11,
	PerCharacterClickBindingsCache = 12,
	GlobalEditModeCache = 13,
	PerCharacterEditModeCache = 14,

	Max = 15,

	AllAccountDataCacheMask = 0x7FFF,
	GlobalCacheMask = 0x2515,
	PerCharacterCacheMask = 0x5AEA
}