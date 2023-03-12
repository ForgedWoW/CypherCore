// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr0 : uint
{
	ProcFailureBurnsCharge = 0x01, /*Nyi*/         // Proc Failure Burns Charge
	UsesRangedSlot = 0x02,                         // Uses Ranged Slot Description Use Ammo, Ranged Attack Range Modifiers, Ranged Haste, Etc.
	OnNextSwingNoDamage = 0x04,                    // On Next Swing (No Damage) Description Both "On Next Swing" Attributes Have Identical Handling In Server & Client
	DoNotLogImmuneMisses = 0x08,                   // Do Not Log Immune Misses (Client Only)
	IsAbility = 0x10,                              // Is Ability Description Cannot Be Reflected, Not Affected By Cast Speed Modifiers, Etc.
	IsTradeskill = 0x20,                           // Is Tradeskill Description Displayed In Recipe List, Not Affected By Cast Speed Modifiers
	Passive = 0x40,                                // Passive Description Spell Is Automatically Cast On Self By Core
	DoNotDisplaySpellbookAuraIconCombatLog = 0x80, // Do Not Display (Spellbook, Aura Icon, Combat Log) (Client Only) Description Not Visible In Spellbook Or Aura Bar
	DoNotLog = 0x100,                              // Do Not Log (Client Only) Description Spell Will Not Appear In Combat Logs
	HeldItemOnly = 0x200,                          // Held Item Only (Client Only) Description Client Will Automatically Select Main-Hand Item As Cast Target
	OnNextSwing = 0x400,                           // On Next Swing Description Both "On Next Swing" Attributes Have Identical Handling In Server & Client
	WearerCastsProcTrigger = 0x800, /*Nyi*/        // Wearer Casts Proc Trigger
	ServerOnly = 0x1000,                           // Server Only
	AllowItemSpellInPvp = 0x2000,                  // Allow Item Spell In Pvp
	OnlyIndoors = 0x4000,                          // Only Indoors
	OnlyOutdoors = 0x8000,                         // Only Outdoors
	NotShapeshifted = 0x10000,                     // Not Shapeshifted
	OnlyStealthed = 0x20000,                       // Only Stealthed
	DoNotSheath = 0x40000,                         // Do Not Sheath (Client Only)
	ScalesWithCreatureLevel = 0x80000,             // Scales W/ Creature Level Description For Non-Player Casts, Scale Impact And Power Cost With Caster'S Level
	CancelsAutoAttackCombat = 0x100000,            // Cancels Auto Attack Combat Description After Casting This, The Current Auto-Attack Will Be Interrupted
	NoActiveDefense = 0x200000,                    // No Active Defense Description Spell Cannot Be Dodged, Parried Or Blocked
	TrackTargetInCastPlayerOnly = 0x400000,        // Track Target In Cast (Player Only) (Client Only)
	AllowCastWhileDead = 0x800000,                 // Allow Cast While Dead Description Spells Without This Flag Cannot Be Cast By Dead Units In Non-Triggered Contexts
	AllowWhileMounted = 0x1000000,                 // Allow While Mounted
	CooldownOnEvent = 0x2000000,                   // Cooldown On Event Description Spell Is Unusable While Already Active, And Cooldown Does Not Begin Until The Effects Have Worn Off
	AuraIsDebuff = 0x4000000,                      // Aura Is Debuff Description Forces The Spell To Be Treated As A Negative Spell
	AllowWhileSitting = 0x8000000,                 // Allow While Sitting
	NotInCombatOnlyPeaceful = 0x10000000,          // Not In Combat (Only Peaceful)
	NoImmunities = 0x20000000,                     // No Immunities Description Allows Spell To Pierce Invulnerability, Unless The Invulnerability Spell Also Has This Attribute
	HeartbeatResist = 0x40000000,                  // Heartbeat Resist Description Periodically Re-Rolls Against Resistance To Potentially Expire Aura Early
	NoAuraCancel = 0x80000000                      // No Aura Cancel Description Prevents The Player From Voluntarily Canceling A Positive Aura
}