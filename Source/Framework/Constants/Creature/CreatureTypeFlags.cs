// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureTypeFlags : uint
{
	Tameable = 0x00000001,        // Makes The Mob Tameable (Must Also Be A Beast And Have Family Set)
	VisibleToGhosts = 0x00000002, // Creature Is Also Visible For Not Alive Player. Allows Gossip Interaction If Npcflag Allows?
	BossMob = 0x00000004,         // Changes Creature'S Visible Level To "??" In The Creature'S Portrait - Immune Knockback.
	DoNotPlayWoundAnim = 0x00000008,
	NoFactionTooltip = 0x00000010,
	MoreAudible = 0x00000020, // Sound Related
	SpellAttackable = 0x00000040,
	InteractWhileDead = 0x00000080,  // Player Can Interact With The Creature If Creature Is Dead (Not If Player Is Dead)
	SkinWithHerbalism = 0x00000100,  // Can Be Looted By Herbalist
	SkinWithMining = 0x00000200,     // Can Be Looted By Miner
	NoDeathMessage = 0x00000400,     // Death Event Will Not Show Up In Combat Log
	AllowMountedCombat = 0x00000800, // Creature Can Remain Mounted When Entering Combat
	CanAssist = 0x00001000,          // ? Can Aid Any Player In Combat If In Range?
	NoPetBar = 0x00002000,
	MaskUid = 0x00004000,
	SkinWithEngineering = 0x00008000,   // Can Be Looted By Engineer
	TameableExotic = 0x00010000,        // Can Be Tamed By Hunter As Exotic Pet
	UseModelCollisionSize = 0x00020000, // Collision Related. (Always Using Default Collision Box?)
	AllowInteractionWhileInCombat = 0x00040000,
	CollideWithMissiles = 0x00080000, // Projectiles Can Collide With This Creature - Interacts With TargetDestTraj
	NoNamePlate = 0x00100000,
	DoNotPlayMountedAnimations = 0x00200000,
	LinkAll = 0x00400000,
	InteractOnlyWithCreator = 0x00800000,
	DoNotPlayUnitEventSounds = 0x01000000,
	HasNoShadowBlob = 0x02000000,
	TreatAsRaidUnit = 0x04000000, //! Creature Can Be Targeted By Spells That Require Target To Be In Caster'S Party/Raid
	ForceGossip = 0x08000000,     // Allows The Creature To Display A Single Gossip Option.
	DoNotSheathe = 0x10000000,
	DoNotTargetOnInteraction = 0x20000000,
	DoNotRenderObjectName = 0x40000000,
	QuestBoss = 0x80000000 // Not Verified
}