// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum eScriptFlags
{
	// Talk Flags
	TalkUsePlayer = 0x1,

	// Emote Flags
	EmoteUseState = 0x1,

	// Teleportto Flags
	TeleportUseCreature = 0x1,

	// Killcredit Flags
	KillcreditRewardGroup = 0x1,

	// Removeaura Flags
	RemoveauraReverse = 0x1,

	// Castspell Flags
	CastspellSourceToTarget = 0,
	CastspellSourceToSource = 1,
	CastspellTargetToTarget = 2,
	CastspellTargetToSource = 3,
	CastspellSearchCreature = 4,
	CastspellTriggered = 0x1,

	// Playsound Flags
	PlaysoundTargetPlayer = 0x1,
	PlaysoundDistanceSound = 0x2,

	// Orientation Flags
	OrientationFaceTarget = 0x1
}