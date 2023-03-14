// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.m_Events.PilgrimsBounty;

internal struct SpellIds
{
	//Pilgrims Bounty
	public const uint WellFedApTrigger = 65414;
	public const uint WellFedZmTrigger = 65412;
	public const uint WellFedHitTrigger = 65416;
	public const uint WellFedHasteTrigger = 65410;
	public const uint WellFedSpiritTrigger = 65415;

	//FeastOnSpells
	//Feastonspells
	public const uint FeastOnTurkey = 61784;
	public const uint FeastOnCranberries = 61785;
	public const uint FeastOnSweetPotatoes = 61786;
	public const uint FeastOnPie = 61787;
	public const uint FeastOnStuffing = 61788;
	public const uint CranberryHelpins = 61841;
	public const uint TurkeyHelpins = 61842;
	public const uint StuffingHelpins = 61843;
	public const uint SweetPotatoHelpins = 61844;
	public const uint PieHelpins = 61845;
	public const uint OnPlateEatVisual = 61826;

	//Theturkinator
	public const uint KillCounterVisual = 62015;
	public const uint KillCounterVisualMax = 62021;

	//Spiritofsharing
	public const uint TheSpiritOfSharing = 61849;

	//Bountifultablemisc
	public const uint OnPlateTurkey = 61928;
	public const uint OnPlateCranberries = 61925;
	public const uint OnPlateStuffing = 61927;
	public const uint OnPlateSweetPotatoes = 61929;
	public const uint OnPlatePie = 61926;
	public const uint PassTheTurkey = 66373;
	public const uint PassTheCranberries = 66372;
	public const uint PassTheStuffing = 66375;
	public const uint PassTheSweetPotatoes = 66376;
	public const uint PassThePie = 66374;
	public const uint OnPlateVisualPie = 61825;
	public const uint OnPlateVisualCranberries = 61821;
	public const uint OnPlateVisualPotatoes = 61824;
	public const uint OnPlateVisualTurkey = 61822;
	public const uint OnPlateVisualStuffing = 61823;
	public const uint AServingOfCranberriesPlate = 61833;
	public const uint AServingOfTurkeyPlate = 61835;
	public const uint AServingOfStuffingPlate = 61836;
	public const uint AServingOfSweetPotatoesPlate = 61837;
	public const uint AServingOfPiePlate = 61838;
	public const uint AServingOfCranberriesChair = 61804;
	public const uint AServingOfTurkeyChair = 61807;
	public const uint AServingOfStuffingChair = 61806;
	public const uint AServingOfSweetPotatoesChair = 61808;
	public const uint AServingOfPieChair = 61805;
}

internal struct CreatureIds
{
	//BountifulTableMisc
	public const uint BountifulTable = 32823;
}

internal struct EmoteIds
{
	//TheTurkinator
	public const uint TurkeyHunter = 0;
	public const uint TurkeyDomination = 1;
	public const uint TurkeySlaughter = 2;
	public const uint TurkeyTriumph = 3;
}

internal struct SeatIds
{
	//BountifulTableMisc
	public const sbyte Player = 0;
	public const sbyte PlateHolder = 6;
}

[Script("spell_gen_slow_roasted_turkey", SpellIds.WellFedApTrigger)]
[Script("spell_gen_cranberry_chutney", SpellIds.WellFedZmTrigger)]
[Script("spell_gen_spice_bread_stuffing", SpellIds.WellFedHitTrigger)]
[Script("spell_gen_pumpkin_pie", SpellIds.WellFedSpiritTrigger)]
[Script("spell_gen_candied_sweet_potato", SpellIds.WellFedHasteTrigger)]
internal class spell_pilgrims_bounty_buff_food : AuraScript, IHasAuraEffects
{
	private readonly uint _triggeredSpellId;

	private bool _handled;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_pilgrims_bounty_buff_food(uint triggeredSpellId)
	{
		_triggeredSpellId = triggeredSpellId;
		_handled = false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 2, AuraType.PeriodicTriggerSpell));
	}

	private void HandleTriggerSpell(AuraEffect aurEff)
	{
		PreventDefaultAction();

		if (_handled)
			return;

		_handled = true;
		Target.CastSpell(Target, _triggeredSpellId, true);
	}
}

/* 61784 - Feast On Turkey
 * 61785 - Feast On Cranberries
 * 61786 - Feast On Sweet Potatoes
 * 61787 - Feast On Pie
 * 61788 - Feast On Stuffing */
[Script]
internal class spell_pilgrims_bounty_feast_on_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;

		uint _spellId = 0;

		switch (SpellInfo.Id)
		{
			case SpellIds.FeastOnTurkey:
				_spellId = SpellIds.TurkeyHelpins;

				break;
			case SpellIds.FeastOnCranberries:
				_spellId = SpellIds.CranberryHelpins;

				break;
			case SpellIds.FeastOnSweetPotatoes:
				_spellId = SpellIds.SweetPotatoHelpins;

				break;
			case SpellIds.FeastOnPie:
				_spellId = SpellIds.PieHelpins;

				break;
			case SpellIds.FeastOnStuffing:
				_spellId = SpellIds.StuffingHelpins;

				break;
			default:
				return;
		}

		var vehicle = caster.VehicleKit1;

		if (vehicle != null)
		{
			var target = vehicle.GetPassenger(0);

			if (target != null)
			{
				var player = target.AsPlayer;

				if (player != null)
				{
					player.CastSpell(player, SpellIds.OnPlateEatVisual, true);

					caster.CastSpell(player,
									_spellId,
									new CastSpellExtraArgs(TriggerCastFlags.FullMask)
										.SetOriginalCaster(player.GUID));
				}
			}
		}

		var aura = caster.GetAura((uint)EffectValue);

		if (aura != null)
		{
			if (aura.StackAmount == 1)
				caster.RemoveAura((uint)aura.SpellInfo.GetEffect(0).CalcValue());

			aura.ModStackAmount(-1);
		}
	}
}

[Script] // 62014 - Turkey Tracker
internal class spell_pilgrims_bounty_turkey_tracker_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var caster = Caster.AsCreature;
		var target = HitUnit;

		if (target == null ||
			caster == null)
			return;

		if (target.HasAura(SpellIds.KillCounterVisualMax))
			return;

		var aura = target.GetAura(SpellInfo.Id);

		if (aura != null)
		{
			switch (aura.StackAmount)
			{
				case 10:
					caster.AI.Talk(EmoteIds.TurkeyHunter, target);

					break;
				case 20:
					caster.AI.Talk(EmoteIds.TurkeyDomination, target);

					break;
				case 30:
					caster.AI.Talk(EmoteIds.TurkeySlaughter, target);

					break;
				case 40:
					caster.AI.Talk(EmoteIds.TurkeyTriumph, target);
					target.CastSpell(target, SpellIds.KillCounterVisualMax, true);
					target.RemoveAura(SpellInfo.Id);

					break;
				default:
					return;
			}

			target.CastSpell(target, SpellIds.KillCounterVisual, true);
		}
	}
}

[Script("spell_pilgrims_bounty_well_fed_turkey", SpellIds.WellFedApTrigger)]
[Script("spell_pilgrims_bounty_well_fed_cranberry", SpellIds.WellFedZmTrigger)]
[Script("spell_pilgrims_bounty_well_fed_stuffing", SpellIds.WellFedHitTrigger)]
[Script("spell_pilgrims_bounty_well_fed_sweet_potatoes", SpellIds.WellFedHasteTrigger)]
[Script("spell_pilgrims_bounty_well_fed_pie", SpellIds.WellFedSpiritTrigger)]
internal class spell_pilgrims_bounty_well_fed_SpellScript : SpellScript, IHasSpellEffects
{
	private readonly uint _triggeredSpellId;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public spell_pilgrims_bounty_well_fed_SpellScript(uint triggeredSpellId)
	{
		_triggeredSpellId = triggeredSpellId;
	}


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var target = HitPlayer;

		if (target == null)
			return;

		var aura = target.GetAura(SpellInfo.Id);

		if (aura != null)
			if (aura.StackAmount == 5)
				target.CastSpell(target, _triggeredSpellId, true);

		var turkey = target.GetAura(SpellIds.TurkeyHelpins);
		var cranberies = target.GetAura(SpellIds.CranberryHelpins);
		var stuffing = target.GetAura(SpellIds.StuffingHelpins);
		var sweetPotatoes = target.GetAura(SpellIds.SweetPotatoHelpins);
		var pie = target.GetAura(SpellIds.PieHelpins);

		if ((turkey != null && turkey.StackAmount == 5) &&
			(cranberies != null && cranberies.StackAmount == 5) &&
			(stuffing != null && stuffing.StackAmount == 5) &&
			(sweetPotatoes != null && sweetPotatoes.StackAmount == 5) &&
			(pie != null && pie.StackAmount == 5))
		{
			target.CastSpell(target, SpellIds.TheSpiritOfSharing, true);
			target.RemoveAura(SpellIds.TurkeyHelpins);
			target.RemoveAura(SpellIds.CranberryHelpins);
			target.RemoveAura(SpellIds.StuffingHelpins);
			target.RemoveAura(SpellIds.SweetPotatoHelpins);
			target.RemoveAura(SpellIds.PieHelpins);
		}
	}
}

[Script("spell_pilgrims_bounty_on_plate_turkey", SpellIds.OnPlateTurkey, SpellIds.PassTheTurkey, SpellIds.OnPlateVisualTurkey, SpellIds.AServingOfTurkeyChair)]
[Script("spell_pilgrims_bounty_on_plate_cranberries", SpellIds.OnPlateCranberries, SpellIds.PassTheCranberries, SpellIds.OnPlateVisualCranberries, SpellIds.AServingOfCranberriesChair)]
[Script("spell_pilgrims_bounty_on_plate_stuffing", SpellIds.OnPlateStuffing, SpellIds.PassTheStuffing, SpellIds.OnPlateVisualStuffing, SpellIds.AServingOfStuffingChair)]
[Script("spell_pilgrims_bounty_on_plate_sweet_potatoes", SpellIds.OnPlateSweetPotatoes, SpellIds.PassTheSweetPotatoes, SpellIds.OnPlateVisualPotatoes, SpellIds.AServingOfSweetPotatoesChair)]
[Script("spell_pilgrims_bounty_on_plate_pie", SpellIds.OnPlatePie, SpellIds.PassThePie, SpellIds.OnPlateVisualPie, SpellIds.AServingOfPieChair)]
internal class spell_pilgrims_bounty_on_plate_SpellScript : SpellScript, IHasSpellEffects
{
	private readonly uint _triggeredSpellId1;
	private readonly uint _triggeredSpellId2;
	private readonly uint _triggeredSpellId3;
	private readonly uint _triggeredSpellId4;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public spell_pilgrims_bounty_on_plate_SpellScript(uint triggeredSpellId1, uint triggeredSpellId2, uint triggeredSpellId3, uint triggeredSpellId4)
	{
		_triggeredSpellId1 = triggeredSpellId1;
		_triggeredSpellId2 = triggeredSpellId2;
		_triggeredSpellId3 = triggeredSpellId3;
		_triggeredSpellId4 = triggeredSpellId4;
	}


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private Vehicle GetTable(Unit target)
	{
		if (target.IsPlayer)
		{
			var vehBase = target.VehicleBase;

			if (vehBase != null)
			{
				var table = vehBase.Vehicle1;

				if (table != null)
					if (table.GetCreatureEntry() == CreatureIds.BountifulTable)
						return table;
			}
		}
		else
		{
			var veh = target.Vehicle1;

			if (veh != null)
				if (veh.GetCreatureEntry() == CreatureIds.BountifulTable)
					return veh;
		}

		return null;
	}

	private Unit GetPlateInSeat(Vehicle table, sbyte seat)
	{
		var holderUnit = table.GetPassenger(SeatIds.PlateHolder);

		if (holderUnit != null)
		{
			var holder = holderUnit.VehicleKit1;

			if (holder != null)
				return holder.GetPassenger(seat);
		}

		return null;
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;

		if (!target ||
			caster == target)
			return;

		var table = GetTable(caster);

		if (!table ||
			table != GetTable(target))
			return;

		var casterChair = caster.VehicleKit1;

		if (casterChair != null)
		{
			var casterPlr = casterChair.GetPassenger(SeatIds.Player);

			if (casterPlr != null)
			{
				if (casterPlr == target)
					return;

				casterPlr.CastSpell(casterPlr, _triggeredSpellId2, true); //Credit for Sharing is Caring(always)

				var seat = target.TransSeat;

				if (target.IsPlayer &&
					target.VehicleBase)
					seat = target.VehicleBase.TransSeat;

				var plate = GetPlateInSeat(table, seat);

				if (plate != null)
				{
					if (target.IsPlayer) //Food Fight case
					{
						casterPlr.CastSpell(target, _triggeredSpellId1, true);
						caster.CastSpell(target.VehicleBase, _triggeredSpellId4, true); //CanEat-chair(always)
					}
					else
					{
						casterPlr.CastSpell(plate, _triggeredSpellId3, true); //Food Visual on plate
						caster.CastSpell(target, _triggeredSpellId4, true);   //CanEat-chair(always)
					}
				}
			}
		}
	}
}

[Script("spell_pilgrims_bounty_a_serving_of_cranberries", SpellIds.AServingOfCranberriesPlate)]
[Script("spell_pilgrims_bounty_a_serving_of_turkey", SpellIds.AServingOfTurkeyPlate)]
[Script("spell_pilgrims_bounty_a_serving_of_stuffing", SpellIds.AServingOfStuffingPlate)]
[Script("spell_pilgrims_bounty_a_serving_of_potatoes", SpellIds.AServingOfSweetPotatoesPlate)]
[Script("spell_pilgrims_bounty_a_serving_of_pie", SpellIds.AServingOfPiePlate)]
internal class spell_pilgrims_bounty_a_serving_of_AuraScript : AuraScript, IHasAuraEffects
{
	private readonly uint _triggeredSpellId;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_pilgrims_bounty_a_serving_of_AuraScript(uint triggeredSpellId)
	{
		_triggeredSpellId = triggeredSpellId;
	}


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;
		target.CastSpell(target, (uint)aurEff.Amount, true);
		HandlePlate(target, true);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;
		target.RemoveAura((uint)aurEff.Amount);
		HandlePlate(target, false);
	}

	private void HandlePlate(Unit target, bool apply)
	{
		var table = target.Vehicle1;

		if (table != null)
		{
			var holderUnit = table.GetPassenger(SeatIds.PlateHolder);

			if (holderUnit != null)
			{
				var holder = holderUnit.VehicleKit1;

				if (holder != null)
				{
					var plate = holder.GetPassenger(target.TransSeat);

					if (plate != null)
					{
						if (apply)
							target.CastSpell(plate, _triggeredSpellId, true);
						else
							plate.RemoveAura(_triggeredSpellId);
					}
				}
			}
		}
	}
}