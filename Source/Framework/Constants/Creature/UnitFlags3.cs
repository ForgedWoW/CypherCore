// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UnitFlags3 : uint
{
	Unk0 = 0x01,
	UnconsciousOnDeath = 0x02, // Title Unconscious On Death Description Shows "Unconscious" In Unit Tooltip Instead Of "Dead"
	AllowMountedCombat = 0x04, // Title Allow Mounted Combat
	GarrisonPet = 0x08,        // Title Garrison Pet Description Special Garrison Pet Creatures That Display One Of Favorite Player Battle Pets - This Flag Allows Querying Name And Turns Off Default Battle Pet Behavior
	UiCanGetPosition = 0x10,   // Title Ui Can Get Position Description Allows Lua Functions Like Unitposition To Always Get The Position Even For Npcs Or Non-Grouped Players
	AiObstacle = 0x20,
	AlternativeDefaultLanguage = 0x40,
	SuppressAllNpcFeedback = 0x80, // Title Suppress All Npc Feedback Description Skips Playing Sounds On Left Clicking Npc For All Npcs As Long As Npc With This Flag Is Visible
	IgnoreCombat = 0x100,          // Title Ignore Combat Description Same As SpellAuraIgnoreCombat
	SuppressNpcFeedback = 0x200,   // Title Suppress Npc Feedback Description Skips Playing Sounds On Left Clicking Npc
	Unk10 = 0x400,
	Unk11 = 0x800,
	Unk12 = 0x1000,
	FakeDead = 0x2000,                             // Title Show As Dead
	NoFacingOnInteractAndFastFacingChase = 0x4000, // Causes The Creature To Both Not Change Facing On Interaction And Speeds Up Smooth Facing Changes While Attacking (Clientside)
	UntargetableFromUi = 0x8000,                   // Title Untargetable From Ui Description Cannot Be Targeted From Lua Functions Startattack, Targetunit, Petattack
	NoFacingOnInteractWhileFakeDead = 0x10000,     // Prevents Facing Changes While Interacting If Creature Has Flag FakeDead
	AlreadySkinned = 0x20000,
	SuppressAllNpcSounds = 0x40000, // Title Suppress All Npc Sounds Description Skips Playing Sounds On Beginning And End Of Npc Interaction For All Npcs As Long As Npc With This Flag Is Visible
	SuppressNpcSounds = 0x80000,    // Title Suppress Npc Sounds Description Skips Playing Sounds On Beginning And End Of Npc Interaction
	Unk20 = 0x100000,
	Unk21 = 0x200000,
	DontFadeOut = 0x400000,
	Unk23 = 0x800000,
	ForceHideNameplate = 0x1000000,
	Unk25 = 0x2000000,
	Unk26 = 0x4000000,
	Unk27 = 0x8000000,
	Unk28 = 0x10000000,
	Unk29 = 0x20000000,
	Unk30 = 0x40000000,
	Unk31 = 0x80000000,

	Disallowed = (Unk0 | /* UnconsciousOnDeath | */ /* AllowMountedCombat | */ GarrisonPet |
				/* UiCanGetPosition | */ /* AiObstacle | */ AlternativeDefaultLanguage | /* SuppressAllNpcFeedback | */
				IgnoreCombat |
				SuppressNpcFeedback |
				Unk10 |
				Unk11 |
				Unk12 | /* FakeDead | */ /* NoFacingOnInteractAndFastFacingChase | */                 /* UntargetableFromUi | */
				/* NoFacingOnInteractWhileFakeDead | */ AlreadySkinned | /* SuppressAllNpcSounds | */ /* SuppressNpcSounds | */
				Unk20 |
				Unk21 | /* DontFadeOut | */ Unk23 |
				ForceHideNameplate |
				Unk25 |
				Unk26 |
				Unk27 |
				Unk28 |
				Unk29 |
				Unk30 |
				Unk31),                  // Skip
	Allowed = (0xffffffff & ~Disallowed) // Skip
}