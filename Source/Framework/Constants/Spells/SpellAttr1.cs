// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr1 : uint
{
	DismissPetFirst = 0x01,                                       // Dismiss Pet First Description Without This Attribute, Summoning Spells Will Fail If Caster Already Has A Pet
	UseAllMana = 0x02,                                            // Use All Mana Description Ignores Listed Power Cost And Drains Entire Pool Instead
	IsChannelled = 0x04,                                          // Is Channelled Description Both "Channeled" Attributes Have Identical Handling In Server & Client
	NoRedirection = 0x08,                                         // No Redirection Description Spell Will Not Be Attracted By SpellMagnet Auras (Grounding Totem)
	NoSkillIncrease = 0x10,                                       // No Skill Increase
	AllowWhileStealthed = 0x20,                                   // Allow While Stealthed
	IsSelfChannelled = 0x40,                                      // Is Self Channelled Description Both "Channeled" Attributes Have Identical Handling In Server & Client
	NoReflection = 0x80,                                          // No Reflection Description Spell Will Pierce Through Spell Reflection And Similar
	OnlyPeacefulTargets = 0x100,                                  // Only Peaceful Targets Description Target Cannot Be In Combat
	InitiatesCombatEnablesAutoAttack = 0x200,                     // Initiates Combat (Enables Auto-Attack) (Client Only) Description Caster Will Begin Auto-Attacking The Target On Cast
	NoThreat = 0x400,                                             // Does Not Generate Threat Description Also Does Not Cause Target To Engage
	AuraUnique = 0x800,                                           // Aura Unique Description Aura Will Not Refresh Its Duration When Recast
	FailureBreaksStealth = 0x1000,                                // Failure Breaks Stealth
	ToggleFarSight = 0x2000,                                      // Toggle Far Sight (Client Only)
	TrackTargetInChannel = 0x4000,                                // Track Target In Channel Description While Channeling, Adjust Facing To Face Target
	ImmunityPurgesEffect = 0x8000,                                // Immunity Purges Effect Description For Immunity Spells, Cancel All Auras That This Spell Would Make You Immune To When The Spell Is Applied
	ImmunityToHostileAndFriendlyEffects = 0x10000, /*Wrong Impl*/ // Immunity To Hostile & Friendly Effects Description Will Not Pierce Divine Shield, Ice Block And Other Full Invulnerabilities
	NoAutocastAi = 0x20000,                                       // No Autocast (Ai)
	PreventsAnim = 0x40000, /*Nyi*/                               // Prevents Anim Description Auras Apply UnitFlagPreventEmotesFromChatText
	ExcludeCaster = 0x80000,                                      // Exclude Caster
	FinishingMoveDamage = 0x100000,                               // Finishing Move - Damage
	ThreatOnlyOnMiss = 0x200000, /*Nyi*/                          // Threat Only On Miss
	FinishingMoveDuration = 0x400000,                             // Finishing Move - Duration
	IgnoreOwnersDeath = 0x800000, /*Nyi*/                         // Ignore Owner'S Death
	SpecialSkillup = 0x1000000,                                   // Special Skillup
	AuraStaysAfterCombat = 0x2000000,                             // Aura Stays After Combat
	RequireAllTargets = 0x4000000, /*Nyi, Unk*/                   // Require All Targets
	DiscountPowerOnMiss = 0x8000000,                              // Discount Power On Miss
	NoAuraIcon = 0x10000000,                                      // No Aura Icon (Client Only)
	NameInChannelBar = 0x20000000,                                // Name In Channel Bar (Client Only)
	DispelAllStacks = 0x40000000,                                 // Dispel All Stacks
	CastWhenLearned = 0x80000000                                  // Cast When Learned
}