// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.AI.PlayerAI;

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

    private readonly bool _isSelfHealer;

    private readonly uint _selfSpec;

    private bool _isSelfRangedAttacker;

    public PlayerAI(Player player) : base(player)
    {
        Me = player;
        _selfSpec = player.GetPrimarySpecialization();
        _isSelfHealer = IsPlayerHealer(player);
        _isSelfRangedAttacker = IsPlayerRangedAttacker(player);
    }

    public void CancelAllShapeshifts()
    {
        var shapeshiftAuras = Me.GetAuraEffectsByType(AuraType.ModShapeshift);
        List<Aura> removableShapeshifts = new();

        foreach (var auraEff in shapeshiftAuras)
        {
            var aura = auraEff.Base;

            var auraInfo = aura?.SpellInfo;

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

    public void DoAutoAttackIfReady()
    {
        if (IsRangedAttacker())
            DoRangedAttackIfReady();
        else
            DoMeleeAttackIfReady();
    }

    public void DoCastAtTarget(Tuple<Spell, Unit> spell)
    {
        SpellCastTargets targets = new()
        {
            UnitTarget = spell.Item2
        };

        spell.Item1.Prepare(targets);
    }

    public Creature GetCharmer()
    {
        return Me.CharmerGUID.IsCreature ? ObjectAccessor.GetCreature(Me, Me.CharmerGUID) : null;
    }

    public uint GetSpec(Player who = null)
    {
        return who == null || who == Me ? _selfSpec : who.GetPrimarySpecialization();
    }

    // helper functions to determine player info
    public bool IsHealer(Player who = null)
    {
        return who == null || who == Me ? _isSelfHealer : IsPlayerHealer(who);
    }

    public bool IsRangedAttacker(Player who = null)
    {
        return who == null || who == Me ? _isSelfRangedAttacker : IsPlayerRangedAttacker(who);
    }

    public virtual Unit SelectAttackTarget()
    {
        return Me.Charmer?.Victim;
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

    public void SetIsRangedAttacker(bool state)
    {
        _isSelfRangedAttacker = state;
    }

    public void VerifyAndPushSpellCast<T>(List<Tuple<Tuple<Spell, Unit>, uint>> spells, uint spellId, T target, uint weight) where T : Unit
    {
        var spell = VerifySpellCast(spellId, target);

        if (spell != null)
            spells.Add(Tuple.Create(spell, weight));
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

                if (pTarget == null)
                    return null;

                break;

            case SpellTarget.Charmer:
                pTarget = Me.Charmer;

                if (pTarget == null)
                    return null;

                break;

            case SpellTarget.Self:
                pTarget = Me;

                break;
        }

        return VerifySpellCast(spellId, pTarget);
    }

    private void DoRangedAttackIfReady()
    {
        if (Me.HasUnitState(UnitState.Casting))
            return;

        if (!Me.IsAttackReady(WeaponAttackType.RangedAttack))
            return;

        var victim = Me.Victim;

        if (victim == null)
            return;

        uint rangedAttackSpell = 0;

        var rangedItem = Me.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.Ranged);
        var rangedTemplate = rangedItem?.Template;

        if (rangedTemplate != null)
            rangedAttackSpell = (ItemSubClassWeapon)rangedTemplate.SubClass switch
            {
                ItemSubClassWeapon.Bow      => Spells.SHOOT,
                ItemSubClassWeapon.Gun      => Spells.SHOOT,
                ItemSubClassWeapon.Crossbow => Spells.SHOOT,
                ItemSubClassWeapon.Thrown   => Spells.THROW,
                ItemSubClassWeapon.Wand     => Spells.WAND,
                _                           => rangedAttackSpell
            };

        if (rangedAttackSpell == 0)
            return;

        var spellInfo = Me.SpellManager.GetSpellInfo(rangedAttackSpell, Me.Location.Map.DifficultyID);

        if (spellInfo == null)
            return;

        var spell = Me.SpellFactory.NewSpell(spellInfo, TriggerCastFlags.CastDirectly);

        if (spell.CheckPetCast(victim) != SpellCastResult.SpellCastOk)
            return;

        SpellCastTargets targets = new()
        {
            UnitTarget = victim
        };

        spell.Prepare(targets);

        Me.ResetAttackTimer(WeaponAttackType.RangedAttack);
    }

    // this allows overriding of the default ranged attacker detection
    private bool IsPlayerHealer(Player who)
    {
        if (who == null)
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

    private bool IsPlayerRangedAttacker(Player who)
    {
        if (who == null)
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

                var rangedTemplate = rangedSlot?.Template;

                return rangedTemplate != null && Convert.ToBoolean((1 << (int)rangedTemplate.SubClass) & (int)ItemSubClassWeapon.MaskRanged);
            }
            case PlayerClass.Priest:
                return who.GetPrimarySpecialization() == TalentSpecialization.PriestShadow;

            case PlayerClass.Shaman:
                return who.GetPrimarySpecialization() == TalentSpecialization.ShamanElemental;

            case PlayerClass.Druid:
                return who.GetPrimarySpecialization() == TalentSpecialization.DruidBalance;
        }
    }

    private Tuple<Spell, Unit> VerifySpellCast(uint spellId, Unit target)
    {
        // Find highest spell rank that we know
        uint knownRank, nextRank;

        if (Me.HasSpell(spellId))
        {
            // this will save us some lookups if the player has the highest rank (expected case)
            knownRank = spellId;
            nextRank = Me.SpellManager.GetNextSpellInChain(spellId);
        }
        else
        {
            knownRank = 0;
            nextRank = Me.SpellManager.GetFirstSpellInChain(spellId);
        }

        while (nextRank != 0 && Me.HasSpell(nextRank))
        {
            knownRank = nextRank;
            nextRank = Me.SpellManager.GetNextSpellInChain(knownRank);
        }

        if (knownRank == 0)
            return null;

        var spellInfo = Me.SpellManager.GetSpellInfo(knownRank, Me.Location.Map.DifficultyID);

        if (spellInfo == null)
            return null;

        if (Me.SpellHistory.HasGlobalCooldown(spellInfo))
            return null;

        var spell = Me.SpellFactory.NewSpell(spellInfo, TriggerCastFlags.None);

        return spell.CanAutoCast(target) ? Tuple.Create(spell, target) : null;
    }
}