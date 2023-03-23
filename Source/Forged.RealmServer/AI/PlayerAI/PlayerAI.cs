// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class PlayerAI : UnitAI
{
	public enum SpellTarget
	{
		None,
		Victim,
		Charmer,
		Self
	}

	protected new Player Me;
	readonly uint _selfSpec;
	readonly bool _isSelfHealer;
	bool _isSelfRangedAttacker;

	public PlayerAI(Player player) : base(player)
	{
		Me = player;
		_selfSpec = player.GetPrimarySpecialization();
		_isSelfHealer = IsPlayerHealer(player);
		_isSelfRangedAttacker = IsPlayerRangedAttacker(player);
	}

	public Tuple<Spell, Unit> VerifySpellCast(uint spellId, SpellTarget target)
	{
		Unit pTarget = null;

		switch (target)
		{
			case SpellTarget.None:
				break;
			case SpellTarget.Victim:
				pTarget = Me.Victim;

				if (!pTarget)
					return null;

				break;
			case SpellTarget.Charmer:
				pTarget = Me.Charmer;

				if (!pTarget)
					return null;

				break;
			case SpellTarget.Self:
				pTarget = Me;

				break;
		}

		return VerifySpellCast(spellId, pTarget);
	}

	public Tuple<Spell, Unit> SelectSpellCast(List<Tuple<Tuple<Spell, Unit>, uint>> spells)
	{
		if (spells.Empty())
			return null;

		uint totalWeights = 0;

		foreach (var wSpell in spells)
			totalWeights += wSpell.Item2;

		Tuple<Spell, Unit> selected = null;
		var randNum = RandomHelper.URand(0, totalWeights - 1);

		foreach (var wSpell in spells)
		{
			if (selected != null)
				//delete wSpell.first.first;
				continue;

			if (randNum < wSpell.Item2)
				selected = wSpell.Item1;
			else
				randNum -= wSpell.Item2;
			//delete wSpell.first.first;
		}

		spells.Clear();

		return selected;
	}

	public void VerifyAndPushSpellCast<T>(List<Tuple<Tuple<Spell, Unit>, uint>> spells, uint spellId, T target, uint weight) where T : Unit
	{
		var spell = VerifySpellCast(spellId, target);

		if (spell != null)
			spells.Add(Tuple.Create(spell, weight));
	}

	public void DoCastAtTarget(Tuple<Spell, Unit> spell)
	{
		SpellCastTargets targets = new();
		targets.UnitTarget = spell.Item2;
		spell.Item1.Prepare(targets);
	}

	public void DoAutoAttackIfReady()
	{
		if (IsRangedAttacker())
			DoRangedAttackIfReady();
		else
			DoMeleeAttackIfReady();
	}

	public void CancelAllShapeshifts()
	{
		var shapeshiftAuras = Me.GetAuraEffectsByType(AuraType.ModShapeshift);
		List<Aura> removableShapeshifts = new();

		foreach (var auraEff in shapeshiftAuras)
		{
			var aura = auraEff.Base;

			if (aura == null)
				continue;

			var auraInfo = aura.SpellInfo;

			if (auraInfo == null)
				continue;

			if (auraInfo.HasAttribute(SpellAttr0.NoAuraCancel))
				continue;

			if (!auraInfo.IsPositive || auraInfo.IsPassive)
				continue;

			removableShapeshifts.Add(aura);
		}

		foreach (var aura in removableShapeshifts)
			Me.RemoveOwnedAura(aura, AuraRemoveMode.Cancel);
	}

	public Creature GetCharmer()
	{
		if (Me.CharmerGUID.IsCreature)
			return ObjectAccessor.GetCreature(Me, Me.CharmerGUID);

		return null;
	}

	// helper functions to determine player info
	public bool IsHealer(Player who = null)
	{
		return (!who || who == Me) ? _isSelfHealer : IsPlayerHealer(who);
	}

	public bool IsRangedAttacker(Player who = null)
	{
		return (!who || who == Me) ? _isSelfRangedAttacker : IsPlayerRangedAttacker(who);
	}

	public uint GetSpec(Player who = null)
	{
		return (who == null || who == Me) ? _selfSpec : who.GetPrimarySpecialization();
	}

	public void SetIsRangedAttacker(bool state)
	{
		_isSelfRangedAttacker = state;
	} // this allows overriding of the default ranged attacker detection

	public virtual Unit SelectAttackTarget()
	{
		return Me.Charmer ? Me.Charmer.Victim : null;
	}

	bool IsPlayerHealer(Player who)
	{
		if (!who)
			return false;

		return who.Class switch
		{
			PlayerClass.Paladin => who.GetPrimarySpecialization() == TalentSpecialization.PaladinHoly,
			PlayerClass.Priest  => who.GetPrimarySpecialization() == TalentSpecialization.PriestDiscipline || who.GetPrimarySpecialization() == TalentSpecialization.PriestHoly,
			PlayerClass.Shaman  => who.GetPrimarySpecialization() == TalentSpecialization.ShamanRestoration,
			PlayerClass.Monk    => who.GetPrimarySpecialization() == TalentSpecialization.MonkMistweaver,
			PlayerClass.Druid   => who.GetPrimarySpecialization() == TalentSpecialization.DruidRestoration,
			_                   => false,
		};
	}

	bool IsPlayerRangedAttacker(Player who)
	{
		if (!who)
			return false;

		switch (who.Class)
		{
			case PlayerClass.Warrior:
			case PlayerClass.Paladin:
			case PlayerClass.Rogue:
			case PlayerClass.Deathknight:
			default:
				return false;
			case PlayerClass.Mage:
			case PlayerClass.Warlock:
				return true;
			case PlayerClass.Hunter:
			{
				// check if we have a ranged weapon equipped
				var rangedSlot = who.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.Ranged);

				var rangedTemplate = rangedSlot ? rangedSlot.Template : null;

				if (rangedTemplate != null)
					if (Convert.ToBoolean((1 << (int)rangedTemplate.SubClass) & (int)ItemSubClassWeapon.MaskRanged))
						return true;

				return false;
			}
			case PlayerClass.Priest:
				return who.GetPrimarySpecialization() == TalentSpecialization.PriestShadow;
			case PlayerClass.Shaman:
				return who.GetPrimarySpecialization() == TalentSpecialization.ShamanElemental;
			case PlayerClass.Druid:
				return who.GetPrimarySpecialization() == TalentSpecialization.DruidBalance;
		}
	}

	Tuple<Spell, Unit> VerifySpellCast(uint spellId, Unit target)
	{
		// Find highest spell rank that we know
		uint knownRank, nextRank;

		if (Me.HasSpell(spellId))
		{
			// this will save us some lookups if the player has the highest rank (expected case)
			knownRank = spellId;
			nextRank = Global.SpellMgr.GetNextSpellInChain(spellId);
		}
		else
		{
			knownRank = 0;
			nextRank = Global.SpellMgr.GetFirstSpellInChain(spellId);
		}

		while (nextRank != 0 && Me.HasSpell(nextRank))
		{
			knownRank = nextRank;
			nextRank = Global.SpellMgr.GetNextSpellInChain(knownRank);
		}

		if (knownRank == 0)
			return null;

		var spellInfo = Global.SpellMgr.GetSpellInfo(knownRank, Me.Map.DifficultyID);

		if (spellInfo == null)
			return null;

		if (Me.SpellHistory.HasGlobalCooldown(spellInfo))
			return null;

		Spell spell = new(Me, spellInfo, TriggerCastFlags.None);

		if (spell.CanAutoCast(target))
			return Tuple.Create(spell, target);

		return null;
	}

	void DoRangedAttackIfReady()
	{
		if (Me.HasUnitState(UnitState.Casting))
			return;

		if (!Me.IsAttackReady(WeaponAttackType.RangedAttack))
			return;

		var victim = Me.Victim;

		if (!victim)
			return;

		uint rangedAttackSpell = 0;

		var rangedItem = Me.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.Ranged);
		var rangedTemplate = rangedItem ? rangedItem.Template : null;

		if (rangedTemplate != null)
			switch ((ItemSubClassWeapon)rangedTemplate.SubClass)
			{
				case ItemSubClassWeapon.Bow:
				case ItemSubClassWeapon.Gun:
				case ItemSubClassWeapon.Crossbow:
					rangedAttackSpell = Spells.Shoot;

					break;
				case ItemSubClassWeapon.Thrown:
					rangedAttackSpell = Spells.Throw;

					break;
				case ItemSubClassWeapon.Wand:
					rangedAttackSpell = Spells.Wand;

					break;
			}

		if (rangedAttackSpell == 0)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(rangedAttackSpell, Me.Map.DifficultyID);

		if (spellInfo == null)
			return;

		Spell spell = new(Me, spellInfo, TriggerCastFlags.CastDirectly);

		if (spell.CheckPetCast(victim) != SpellCastResult.SpellCastOk)
			return;

		SpellCastTargets targets = new();
		targets.UnitTarget = victim;
		spell.Prepare(targets);

		Me.ResetAttackTimer(WeaponAttackType.RangedAttack);
	}
}