﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.m_Events.Brewfest;

internal struct SpellIds
{
	//Ramblabla
	public const uint Giddyup = 42924;
	public const uint RentalRacingRam = 43883;
	public const uint SwiftWorkRam = 43880;
	public const uint RentalRacingRamAura = 42146;
	public const uint RamLevelNeutral = 43310;
	public const uint RamTrot = 42992;
	public const uint RamCanter = 42993;
	public const uint RamGallop = 42994;
	public const uint RamFatigue = 43052;
	public const uint ExhaustedRam = 43332;
	public const uint RelayRaceTurnIn = 44501;

	//Brewfestmounttransformation
	public const uint MountRam100 = 43900;
	public const uint MountRam60 = 43899;
	public const uint MountKodo100 = 49379;
	public const uint MountKodo60 = 49378;
	public const uint BrewfestMountTransform = 49357;
	public const uint BrewfestMountTransformReverse = 52845;
}

internal struct QuestIds
{
	//Ramblabla
	public const uint BrewfestSpeedBunnyGreen = 43345;
	public const uint BrewfestSpeedBunnyYellow = 43346;
	public const uint BrewfestSpeedBunnyRed = 43347;

	//Barkerbunny
	// Horde
	public const uint BarkForDrohnsDistillery = 11407;
	public const uint BarkForTchalisVoodooBrewery = 11408;

	// Alliance
	public const uint BarkBarleybrew = 11293;
	public const uint BarkForThunderbrews = 11294;
}

internal struct TextIds
{
	// Bark For Drohn'S Distillery!
	public const uint DrohnDistillery1 = 23520;
	public const uint DrohnDistillery2 = 23521;
	public const uint DrohnDistillery3 = 23522;
	public const uint DrohnDistillery4 = 23523;

	// Bark For T'Chali'S Voodoo Brewery!
	public const uint TChalisVoodoo1 = 23524;
	public const uint TChalisVoodoo2 = 23525;
	public const uint TChalisVoodoo3 = 23526;
	public const uint TChalisVoodoo4 = 23527;

	// Bark For The Barleybrews!
	public const uint Barleybrew1 = 23464;
	public const uint Barleybrew2 = 23465;
	public const uint Barleybrew3 = 23466;
	public const uint Barleybrew4 = 22941;

	// Bark For The Thunderbrews!
	public const uint Thunderbrews1 = 23467;
	public const uint Thunderbrews2 = 23468;
	public const uint Thunderbrews3 = 23469;
	public const uint Thunderbrews4 = 22942;
}

[Script] // 42924 - Giddyup!
internal class spell_brewfest_giddyup : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void OnChange(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;

		if (!target.HasAura(SpellIds.RentalRacingRam) &&
			!target.HasAura(SpellIds.SwiftWorkRam))
		{
			target.RemoveAura(Id);

			return;
		}

		if (target.HasAura(SpellIds.ExhaustedRam))
			return;

		switch (StackAmount)
		{
			case 1: // green
				target.RemoveAura(SpellIds.RamLevelNeutral);
				target.RemoveAura(SpellIds.RamCanter);
				target.CastSpell(target, SpellIds.RamTrot, true);

				break;
			case 6: // yellow
				target.RemoveAura(SpellIds.RamTrot);
				target.RemoveAura(SpellIds.RamGallop);
				target.CastSpell(target, SpellIds.RamCanter, true);

				break;
			case 11: // red
				target.RemoveAura(SpellIds.RamCanter);
				target.CastSpell(target, SpellIds.RamGallop, true);

				break;
			default:
				break;
		}

		if (TargetApplication.RemoveMode == AuraRemoveMode.Default)
		{
			target.RemoveAura(SpellIds.RamTrot);
			target.CastSpell(target, SpellIds.RamLevelNeutral, true);
		}
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		Target.RemoveAuraFromStack(Id);
	}
}

// 43310 - Ram Level - Neutral
// 42992 - Ram - Trot
// 42993 - Ram - Canter
// 42994 - Ram - Gallop
[Script]
internal class spell_brewfest_ram : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 1, AuraType.PeriodicDummy));
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		var target = Target;

		if (target.HasAura(SpellIds.ExhaustedRam))
			return;

		switch (Id)
		{
			case SpellIds.RamLevelNeutral:
			{
				var aura = target.GetAura(SpellIds.RamFatigue);

				aura?.ModStackAmount(-4);
			}

				break;
			case SpellIds.RamTrot: // green
			{
				var aura = target.GetAura(SpellIds.RamFatigue);

				aura?.ModStackAmount(-2);

				if (aurEff.GetTickNumber() == 4)
					target.CastSpell(target, QuestIds.BrewfestSpeedBunnyGreen, true);
			}

				break;
			case SpellIds.RamCanter:
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.AddSpellMod(SpellValueMod.AuraStack, 1);
				target.CastSpell(target, SpellIds.RamFatigue, args);

				if (aurEff.GetTickNumber() == 8)
					target.CastSpell(target, QuestIds.BrewfestSpeedBunnyYellow, true);

				break;
			}
			case SpellIds.RamGallop:
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.AddSpellMod(SpellValueMod.AuraStack, target.HasAura(SpellIds.RamFatigue) ? 4 : 5 /*Hack*/);
				target.CastSpell(target, SpellIds.RamFatigue, args);

				if (aurEff.GetTickNumber() == 8)
					target.CastSpell(target, QuestIds.BrewfestSpeedBunnyRed, true);

				break;
			}
			default:
				break;
		}
	}
}

[Script] // 43052 - Ram Fatigue
internal class spell_brewfest_ram_fatigue : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;

		if (StackAmount == 101)
		{
			target.RemoveAura(SpellIds.RamLevelNeutral);
			target.RemoveAura(SpellIds.RamTrot);
			target.RemoveAura(SpellIds.RamCanter);
			target.RemoveAura(SpellIds.RamGallop);
			target.RemoveAura(SpellIds.Giddyup);

			target.CastSpell(target, SpellIds.ExhaustedRam, true);
		}
	}
}

[Script] // 43450 - Brewfest - apple trap - friendly DND
internal class spell_brewfest_apple_trap : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ForceReaction, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.RemoveAura(SpellIds.RamFatigue);
	}
}

[Script] // 43332 - Exhausted Ram
internal class spell_brewfest_exhausted_ram : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;
		target.CastSpell(target, SpellIds.RamLevelNeutral, true);
	}
}

[Script] // 43714 - Brewfest - Relay Race - Intro - Force - Player to throw- DND
internal class spell_brewfest_relay_race_intro_force_player_to_throw : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleForceCast, 0, SpellEffectName.ForceCast, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleForceCast(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		// All this spells trigger a spell that requires reagents; if the
		// triggered spell is cast as "triggered", reagents are not consumed
		HitUnit.CastSpell((Unit)null, EffectInfo.TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask & ~TriggerCastFlags.IgnorePowerAndReagentCost));
	}
}

[Script] // 43755 - Brewfest - Daily - Relay Race - Player - Increase Mount Duration - DND
internal class spell_brewfest_relay_race_turn_in : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		var aura = HitUnit.GetAura(SpellIds.SwiftWorkRam);

		if (aura != null)
		{
			aura.SetDuration(aura.Duration + 30 * Time.InMilliseconds);
			Caster.CastSpell(HitUnit, SpellIds.RelayRaceTurnIn, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		}
	}
}

[Script] // 43876 - Dismount Ram
internal class spell_brewfest_dismount_ram : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		Caster.RemoveAura(SpellIds.RentalRacingRam);
	}
}

// 43259 Brewfest  - Barker Bunny 1
// 43260 Brewfest  - Barker Bunny 2
// 43261 Brewfest  - Barker Bunny 3
// 43262 Brewfest  - Barker Bunny 4
[Script]
internal class spell_brewfest_barker_bunny : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return OwnerAsUnit.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target.AsPlayer;

		uint BroadcastTextId = 0;

		if (target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Incomplete ||
			target.GetQuestStatus(QuestIds.BarkForDrohnsDistillery) == QuestStatus.Complete)
			BroadcastTextId = RandomHelper.RAND(TextIds.DrohnDistillery1, TextIds.DrohnDistillery2, TextIds.DrohnDistillery3, TextIds.DrohnDistillery4);

		if (target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Incomplete ||
			target.GetQuestStatus(QuestIds.BarkForTchalisVoodooBrewery) == QuestStatus.Complete)
			BroadcastTextId = RandomHelper.RAND(TextIds.TChalisVoodoo1, TextIds.TChalisVoodoo2, TextIds.TChalisVoodoo3, TextIds.TChalisVoodoo4);

		if (target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Incomplete ||
			target.GetQuestStatus(QuestIds.BarkBarleybrew) == QuestStatus.Complete)
			BroadcastTextId = RandomHelper.RAND(TextIds.Barleybrew1, TextIds.Barleybrew2, TextIds.Barleybrew3, TextIds.Barleybrew4);

		if (target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Incomplete ||
			target.GetQuestStatus(QuestIds.BarkForThunderbrews) == QuestStatus.Complete)
			BroadcastTextId = RandomHelper.RAND(TextIds.Thunderbrews1, TextIds.Thunderbrews2, TextIds.Thunderbrews3, TextIds.Thunderbrews4);

		if (BroadcastTextId != 0)
			target.Talk(BroadcastTextId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
	}
}

[Script]
internal class spell_item_brewfest_mount_transformation : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster.AsPlayer;

		if (caster.HasAuraType(AuraType.Mounted))
		{
			caster.RemoveAurasByType(AuraType.Mounted);
			uint spell_id;

			switch (SpellInfo.Id)
			{
				case SpellIds.BrewfestMountTransform:
					if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
						spell_id = caster.Team == TeamFaction.Alliance ? SpellIds.MountRam100 : SpellIds.MountKodo100;
					else
						spell_id = caster.Team == TeamFaction.Alliance ? SpellIds.MountRam60 : SpellIds.MountKodo60;

					break;
				case SpellIds.BrewfestMountTransformReverse:
					if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
						spell_id = caster.Team == TeamFaction.Horde ? SpellIds.MountRam100 : SpellIds.MountKodo100;
					else
						spell_id = caster.Team == TeamFaction.Horde ? SpellIds.MountRam60 : SpellIds.MountKodo60;

					break;
				default:
					return;
			}

			caster.CastSpell(caster, spell_id, true);
		}
	}
}