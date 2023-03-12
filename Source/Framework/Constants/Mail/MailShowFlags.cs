// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MailShowFlags
{
	Unk0 = 0x0001,
	Delete = 0x0002,  // Forced Show Delete Button Instead Return Button
	Auction = 0x0004, // From Old Comment
	Unk2 = 0x0008,    // Unknown, Cod Will Be Shown Even Without That Flag
	Return = 0x0010
}