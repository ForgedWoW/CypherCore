// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellCastTargetFlags
{
	None = 0x0,
	Unused1 = 0x01,           // Not Used
	Unit = 0x02,              // Pguid
	UnitRaid = 0x04,          // Not Sent, Used To Validate Target (If Raid Member)
	UnitParty = 0x08,         // Not Sent, Used To Validate Target (If Party Member)
	Item = 0x10,              // Pguid
	SourceLocation = 0x20,    // Pguid, 3 Float
	DestLocation = 0x40,      // Pguid, 3 Float
	UnitEnemy = 0x80,         // Not Sent, Used To Validate Target (If Enemy)
	UnitAlly = 0x100,         // Not Sent, Used To Validate Target (If Ally)
	CorpseEnemy = 0x200,      // Pguid
	UnitDead = 0x400,         // Not Sent, Used To Validate Target (If Dead Creature)
	Gameobject = 0x800,       // Pguid, Used With TargetGameobjectTarget
	TradeItem = 0x1000,       // Pguid
	String = 0x2000,          // String
	GameobjectItem = 0x4000,  // Not Sent, Used With TargetGameobjectItemTarget
	CorpseAlly = 0x8000,      // Pguid
	UnitMinipet = 0x10000,    // Pguid, Used To Validate Target (If Non Combat Pet)
	GlyphSlot = 0x20000,      // Used In Glyph Spells
	DestTarget = 0x40000,     // Sometimes Appears With DestTarget Spells (May Appear Or Not For A Given Spell)
	ExtraTargets = 0x80000,   // Uint32 Counter, Loop { Vec3 - Screen Position (?), Guid }, Not Used So Far
	UnitPassenger = 0x100000, // Guessed, Used To Validate Target (If Vehicle Passenger)\
	Unk400000 = 0x400000,
	Unk1000000 = 0X01000000,
	Unk4000000 = 0X04000000,
	Unk10000000 = 0X10000000,
	Unk40000000 = 0X40000000,

	UnitMask = Unit | UnitRaid | UnitParty | UnitEnemy | UnitAlly | UnitDead | UnitMinipet | UnitPassenger,
	GameobjectMask = Gameobject | GameobjectItem,
	CorpseMask = CorpseAlly | CorpseEnemy,
	ItemMask = TradeItem | Item | GameobjectItem
}