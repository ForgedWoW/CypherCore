// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum UnitFlags : uint
{
	ServerControlled = 0x01,
	NonAttackable = 0x02,       // not attackable, set when creature starts to cast spells with SPELL_EFFECT_SPAWN and cast time, removed when spell hits caster, original name is UNIT_FLAG_SPAWNING. Rename when it will be removed from all scripts
	RemoveClientControl = 0x04, // This is a legacy flag used to disable movement player's movement while controlling other units, SMSG_CLIENT_CONTROL replaces this functionality clientside now. CONFUSED and FLEEING flags have the same effect on client movement asDISABLE_MOVE_CONTROL in addition to preventing spell casts/autoattack (they all allow climbing steeper hills and emotes while moving)
	PlayerControlled = 0x08,    //controlled by player, use _IMMUNE_TO_PC instead of _IMMUNE_TO_NPC
	Rename = 0x10,
	Preparation = 0x20,
	Unk6 = 0x40,
	NotAttackable1 = 0x80,
	ImmuneToPc = 0x100,
	ImmuneToNpc = 0x200,
	Looting = 0x400,
	PetInCombat = 0x800,
	PvpEnabling = 0x1000,
	ForceNamePlate = 0x2000, // Force show nameplate, 9.0
	CantSwim = 0x4000,
	CanSwim = 0x8000, // shows swim animation in water
	NonAttackable2 = 0x10000,
	Pacified = 0x20000,
	Stunned = 0x40000,
	InCombat = 0x80000,
	OnTaxi = 0x100000,
	Disarmed = 0x200000,
	Confused = 0x400000,
	Fleeing = 0x800000,
	Possessed = 0x1000000, // under direct client control by a player (possess or vehicle)
	Uninteractible = 0x2000000,
	Skinnable = 0x4000000,
	Mount = 0x8000000,
	Unk28 = 0x10000000,
	PreventEmotesFromChatText = 0x20000000, // Prevent automatically playing emotes from parsing chat text, for example "lol" in /say, ending message with ? or !, or using /yell
	Sheathe = 0x40000000,
	Immune = 0x80000000,

	Disallowed = (ServerControlled |
				NonAttackable |
				RemoveClientControl |
				PlayerControlled |
				Rename |
				Preparation | /* UNIT_FLAG_UNK_6 | */
				NotAttackable1 |
				Looting |
				PetInCombat |
				PvpEnabling |
				NonAttackable2 |
				Pacified |
				Stunned |
				InCombat |
				OnTaxi |
				Disarmed |
				Confused |
				Fleeing |
				Possessed |
				Skinnable |
				Mount |
				Unk28 |
				PreventEmotesFromChatText |
				Sheathe |
				Immune),

	Allowed = (0xFFFFFFFF & ~Disallowed)
}