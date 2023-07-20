// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public void AddOverrideSpell(uint overridenSpellId, uint newSpellId)
    {
        _overrideSpells.Add(overridenSpellId, newSpellId);
    }

    public void AddSpellMod(SpellModifier mod, bool apply)
    {
        Log.Logger.Debug("Player.AddSpellMod {0}", mod.SpellId);

        // First, manipulate our spellmodifier container
        if (apply)
            _spellModifiers[(int)mod.Op][(int)mod.Type].Add(mod);
        else
            _spellModifiers[(int)mod.Op][(int)mod.Type].Remove(mod);

        // Now, send spellmodifier packet
        switch (mod.Type)
        {
            case SpellModType.Flat:
            case SpellModType.Pct:
                if (!IsLoading)
                {
                    var opcode = mod.Type == SpellModType.Flat ? ServerOpcodes.SetFlatSpellModifier : ServerOpcodes.SetPctSpellModifier;
                    SetSpellModifier packet = new(opcode);

                    // @todo Implement sending of bulk modifiers instead of single
                    SpellModifierInfo spellMod = new()
                    {
                        ModIndex = (byte)mod.Op
                    };

                    for (var eff = 0; eff < 128; ++eff)
                    {
                        FlagArray128 mask = new()
                        {
                            [eff / 32] = 1u << (eff % 32)
                        };

                        if (!((mod as SpellModifierByClassMask)?.Mask & mask))
                            continue;

                        SpellModifierData modData = new();

                        if (mod.Type == SpellModType.Flat)
                        {
                            modData.ModifierValue = 0.0f;

                            foreach (var spellModifier in _spellModifiers[(int)mod.Op][(int)SpellModType.Flat])
                            {
                                var spell = (SpellModifierByClassMask)spellModifier;

                                if (spell.Mask & mask)
                                    modData.ModifierValue += spell.Value;
                            }
                        }
                        else
                        {
                            modData.ModifierValue = 1.0f;

                            foreach (var spellModifier in _spellModifiers[(int)mod.Op][(int)SpellModType.Pct])
                            {
                                var spell = (SpellModifierByClassMask)spellModifier;

                                if (spell.Mask & mask)
                                    modData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, spell.Value);
                            }
                        }

                        modData.ClassIndex = (byte)eff;

                        spellMod.ModifierData.Add(modData);
                    }

                    packet.Modifiers.Add(spellMod);

                    SendPacket(packet);
                }

                break;

            case SpellModType.LabelFlat:
                if (apply)
                    AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellFlatModByLabel), (mod as SpellFlatModifierByLabel)?.Value);
                else
                {
                    var firstIndex = ActivePlayerData.SpellFlatModByLabel.FindIndex((mod as SpellFlatModifierByLabel)?.Value);

                    if (firstIndex >= 0)
                        RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellFlatModByLabel), firstIndex);
                }

                break;

            case SpellModType.LabelPct:
                if (apply)
                    AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellPctModByLabel), (mod as SpellPctModifierByLabel)?.Value);
                else
                {
                    var firstIndex = ActivePlayerData.SpellPctModByLabel.FindIndex((mod as SpellPctModifierByLabel)?.Value);

                    if (firstIndex >= 0)
                        RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellPctModByLabel), firstIndex);
                }

                break;
        }
    }

    public void AddStoredAuraTeleportLocation(uint spellId)
    {
        StoredAuraTeleportLocation storedLocation = new()
        {
            Loc = new WorldLocation(Location),
            CurrentState = StoredAuraTeleportLocation.State.Changed
        };

        _storedAuraTeleportLocations[spellId] = storedLocation;
    }

    public void AddTemporarySpell(uint spellId)
    {
        var spell = _spells.LookupByKey(spellId);

        // spell already added - do not do anything
        if (spell != null)
            return;

        PlayerSpell newspell = new()
        {
            State = PlayerSpellState.Temporary,
            Active = true,
            Dependent = false,
            Disabled = false
        };

        _spells[spellId] = newspell;
    }

    public void ApplyEnchantment(Item item, EnchantmentSlot slot, bool apply, bool applyDur = true, bool ignoreCondition = false)
    {
        if (item == null || !item.IsEquipped)
            return;

        if (slot >= EnchantmentSlot.Max)
            return;

        var enchantID = item.GetEnchantmentId(slot);

        if (enchantID == 0)
            return;

        if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var pEnchant))
            return;

        if (!ignoreCondition && pEnchant.ConditionID != 0 && !EnchantmentFitsRequirements(pEnchant.ConditionID, -1))
            return;

        if (pEnchant.MinLevel > Level)
            return;

        if (pEnchant.RequiredSkillID > 0 && pEnchant.RequiredSkillRank > GetSkillValue((SkillType)pEnchant.RequiredSkillID))
            return;

        // If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
        // rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
        if (slot is EnchantmentSlot.Sock1 or EnchantmentSlot.Sock2 or EnchantmentSlot.Sock3)
        {
            if (item.GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
            {
                // Check if the requirements for the prismatic socket are met before applying the gem stats
                var pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId(EnchantmentSlot.Prismatic));

                if (pPrismaticEnchant == null || (pPrismaticEnchant.RequiredSkillID > 0 && pPrismaticEnchant.RequiredSkillRank > GetSkillValue((SkillType)pPrismaticEnchant.RequiredSkillID)))
                    return;
            }

            // Cogwheel gems dont have requirement data set in SpellItemEnchantment.dbc, but they do have it in Item-sparse.db2
            var gem = item.GetGem((ushort)(slot - EnchantmentSlot.Sock1));

            if (gem != null)
            {
                var gemTemplate = GameObjectManager.GetItemTemplate(gem.ItemId);

                if (gemTemplate != null)
                    if (gemTemplate.RequiredSkill != 0 && GetSkillValue((SkillType)gemTemplate.RequiredSkill) < gemTemplate.RequiredSkillRank)
                        return;
            }
        }

        if (!item.IsBroken)
            for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
            {
                var enchantDisplayType = pEnchant.Effect[s];
                uint enchantAmount = pEnchant.EffectPointsMin[s];
                var enchantSpellID = pEnchant.EffectArg[s];

                switch (enchantDisplayType)
                {
                    case ItemEnchantmentType.None:
                        break;

                    case ItemEnchantmentType.CombatSpell:
                        // processed in Player.CastItemCombatSpell
                        break;

                    case ItemEnchantmentType.Damage:
                    {
                        var attackType = PlayerComputators.GetAttackBySlot(item.Slot, item.Template.InventoryType);

                        if (attackType != WeaponAttackType.Max)
                            UpdateDamageDoneMods(attackType, apply ? -1 : (int)slot);
                    }

                        break;

                    case ItemEnchantmentType.EquipSpell:
                        if (enchantSpellID != 0)
                        {
                            if (apply)
                                SpellFactory.CastSpell(item, enchantSpellID);
                            else
                                RemoveAurasDueToItemSpell(enchantSpellID, item.GUID);
                        }

                        break;

                    case ItemEnchantmentType.Resistance:
                        if (pEnchant.ScalingClass != 0)
                        {
                            int scalingClass = pEnchant.ScalingClass;

                            if ((UnitData.MinItemLevel != 0 || UnitData.MaxItemLevel != 0) && pEnchant.ScalingClassRestricted != 0)
                                scalingClass = pEnchant.ScalingClassRestricted;

                            var minLevel = pEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.ScaleAsAGem) ? 1 : 60u;
                            var scalingLevel = Level;
                            var maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.TableRowCount - 1);

                            if (minLevel > Level)
                                scalingLevel = minLevel;
                            else if (maxLevel < Level)
                                scalingLevel = maxLevel;

                            var spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);

                            if (spellScaling != null)
                                enchantAmount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
                        }

                        enchantAmount = Math.Max(enchantAmount, 1u);
                        HandleStatFlatModifier((UnitMods)((uint)UnitMods.ResistanceStart + enchantSpellID), UnitModifierFlatType.Total, enchantAmount, apply);

                        break;

                    case ItemEnchantmentType.Stat:
                    {
                        if (pEnchant.ScalingClass != 0)
                        {
                            int scalingClass = pEnchant.ScalingClass;

                            if ((UnitData.MinItemLevel != 0 || UnitData.MaxItemLevel != 0) && pEnchant.ScalingClassRestricted != 0)
                                scalingClass = pEnchant.ScalingClassRestricted;

                            var minLevel = pEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.ScaleAsAGem) ? 1 : 60u;
                            var scalingLevel = Level;
                            var maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.TableRowCount - 1);

                            if (minLevel > Level)
                                scalingLevel = minLevel;
                            else if (maxLevel < Level)
                                scalingLevel = maxLevel;

                            var spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);

                            if (spellScaling != null)
                                enchantAmount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
                        }

                        enchantAmount = Math.Max(enchantAmount, 1u);

                        Log.Logger.Debug("Adding {0} to stat nb {1}", enchantAmount, enchantSpellID);

                        switch ((ItemModType)enchantSpellID)
                        {
                            case ItemModType.Mana:
                                Log.Logger.Debug("+ {0} MANA", enchantAmount);
                                HandleStatFlatModifier(UnitMods.Mana, UnitModifierFlatType.Base, enchantAmount, apply);

                                break;

                            case ItemModType.Health:
                                Log.Logger.Debug("+ {0} HEALTH", enchantAmount);
                                HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, enchantAmount, apply);

                                break;

                            case ItemModType.Agility:
                                Log.Logger.Debug("+ {0} AGILITY", enchantAmount);
                                HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Total, enchantAmount, apply);
                                UpdateStatBuffMod(Stats.Agility);

                                break;

                            case ItemModType.Strength:
                                Log.Logger.Debug("+ {0} STRENGTH", enchantAmount);
                                HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Total, enchantAmount, apply);
                                UpdateStatBuffMod(Stats.Strength);

                                break;

                            case ItemModType.Intellect:
                                Log.Logger.Debug("+ {0} INTELLECT", enchantAmount);
                                HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Total, enchantAmount, apply);
                                UpdateStatBuffMod(Stats.Intellect);

                                break;
                            //case ItemModType.Spirit:
                            //Log.Logger.Debug("+ {0} SPIRIT", enchant_amount);
                            //HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.TotalValue, enchant_amount, apply);
                            //ApplyStatBuffMod(Stats.Spirit, enchant_amount, apply);
                            //break;
                            case ItemModType.Stamina:
                                Log.Logger.Debug("+ {0} STAMINA", enchantAmount);
                                HandleStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Total, enchantAmount, apply);
                                UpdateStatBuffMod(Stats.Stamina);

                                break;

                            case ItemModType.DefenseSkillRating:
                                ApplyRatingMod(CombatRating.DefenseSkill, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} DEFENSE", enchantAmount);

                                break;

                            case ItemModType.DodgeRating:
                                ApplyRatingMod(CombatRating.Dodge, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} DODGE", enchantAmount);

                                break;

                            case ItemModType.ParryRating:
                                ApplyRatingMod(CombatRating.Parry, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} PARRY", enchantAmount);

                                break;

                            case ItemModType.BlockRating:
                                ApplyRatingMod(CombatRating.Block, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} SHIELD_BLOCK", enchantAmount);

                                break;

                            case ItemModType.HitMeleeRating:
                                ApplyRatingMod(CombatRating.HitMelee, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} MELEE_HIT", enchantAmount);

                                break;

                            case ItemModType.HitRangedRating:
                                ApplyRatingMod(CombatRating.HitRanged, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} RANGED_HIT", enchantAmount);

                                break;

                            case ItemModType.HitSpellRating:
                                ApplyRatingMod(CombatRating.HitSpell, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} SPELL_HIT", enchantAmount);

                                break;

                            case ItemModType.CritMeleeRating:
                                ApplyRatingMod(CombatRating.CritMelee, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} MELEE_CRIT", enchantAmount);

                                break;

                            case ItemModType.CritRangedRating:
                                ApplyRatingMod(CombatRating.CritRanged, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} RANGED_CRIT", enchantAmount);

                                break;

                            case ItemModType.CritSpellRating:
                                ApplyRatingMod(CombatRating.CritSpell, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} SPELL_CRIT", enchantAmount);

                                break;

                            case ItemModType.HasteSpellRating:
                                ApplyRatingMod(CombatRating.HasteSpell, (int)enchantAmount, apply);

                                break;

                            case ItemModType.HitRating:
                                ApplyRatingMod(CombatRating.HitMelee, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.HitRanged, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.HitSpell, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} HIT", enchantAmount);

                                break;

                            case ItemModType.CritRating:
                                ApplyRatingMod(CombatRating.CritMelee, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.CritRanged, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.CritSpell, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} CRITICAL", enchantAmount);

                                break;

                            case ItemModType.ResilienceRating:
                                ApplyRatingMod(CombatRating.ResiliencePlayerDamage, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} RESILIENCE", enchantAmount);

                                break;

                            case ItemModType.HasteRating:
                                ApplyRatingMod(CombatRating.HasteMelee, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.HasteRanged, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.HasteSpell, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} HASTE", enchantAmount);

                                break;

                            case ItemModType.ExpertiseRating:
                                ApplyRatingMod(CombatRating.Expertise, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} EXPERTISE", enchantAmount);

                                break;

                            case ItemModType.AttackPower:
                                HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, enchantAmount, apply);
                                HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, enchantAmount, apply);
                                Log.Logger.Debug("+ {0} ATTACK_POWER", enchantAmount);

                                break;

                            case ItemModType.RangedAttackPower:
                                HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, enchantAmount, apply);
                                Log.Logger.Debug("+ {0} RANGED_ATTACK_POWER", enchantAmount);

                                break;

                            case ItemModType.ManaRegeneration:
                                ApplyManaRegenBonus((int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} MANA_REGENERATION", enchantAmount);

                                break;

                            case ItemModType.ArmorPenetrationRating:
                                ApplyRatingMod(CombatRating.ArmorPenetration, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} ARMOR PENETRATION", enchantAmount);

                                break;

                            case ItemModType.SpellPower:
                                ApplySpellPowerBonus((int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} SPELL_POWER", enchantAmount);

                                break;

                            case ItemModType.HealthRegen:
                                ApplyHealthRegenBonus((int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} HEALTH_REGENERATION", enchantAmount);

                                break;

                            case ItemModType.SpellPenetration:
                                ApplySpellPenetrationBonus((int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} SPELL_PENETRATION", enchantAmount);

                                break;

                            case ItemModType.BlockValue:
                                HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, enchantAmount, apply);
                                Log.Logger.Debug("+ {0} BLOCK_VALUE", enchantAmount);

                                break;

                            case ItemModType.MasteryRating:
                                ApplyRatingMod(CombatRating.Mastery, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} MASTERY", enchantAmount);

                                break;

                            case ItemModType.Versatility:
                                ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)enchantAmount, apply);
                                ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)enchantAmount, apply);
                                Log.Logger.Debug("+ {0} VERSATILITY", enchantAmount);

                                break;
                        }

                        break;
                    }
                    case ItemEnchantmentType.Totem: // Shaman Rockbiter Weapon
                    {
                        var attackType = PlayerComputators.GetAttackBySlot(item.Slot, item.Template.InventoryType);

                        if (attackType != WeaponAttackType.Max)
                            UpdateDamageDoneMods(attackType, apply ? -1 : (int)slot);

                        break;
                    }
                    case ItemEnchantmentType.UseSpell:
                        // processed in Player.CastItemUseSpell
                        break;

                    case ItemEnchantmentType.PrismaticSocket:
                    case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                    case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                    case ItemEnchantmentType.BonusListID:
                    case ItemEnchantmentType.BonusListCurve:
                    case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                        // nothing do..
                        break;

                    default:
                        Log.Logger.Error("Unknown item enchantment (id = {0}) display type: {1}", enchantID, enchantDisplayType);

                        break;
                }
            }

        // visualize enchantment at player and equipped items
        if (slot == EnchantmentSlot.Perm)
        {
            var visibleItem = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.VisibleItems, item.Slot);
            SetUpdateFieldValue(visibleItem.ModifyValue(visibleItem.ItemVisual), item.GetVisibleItemVisual(this));
        }

        if (applyDur)
        {
            if (apply)
            {
                // set duration
                var duration = item.GetEnchantmentDuration(slot);

                if (duration > 0)
                    AddEnchantmentDuration(item, slot, duration);
            }
            else
                // duration == 0 will remove EnchantDuration
                AddEnchantmentDuration(item, slot, 0);
        }
    }

    public void ApplyItemDependentAuras(Item item, bool apply)
    {
        if (apply)
        {
            var spells = GetSpellMap();

            foreach (var pair in spells)
            {
                if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                    continue;

                var spellInfo = SpellManager.GetSpellInfo(pair.Key);

                if (spellInfo == null || !spellInfo.IsPassive || spellInfo.EquippedItemClass < 0)
                    continue;

                if (!HasAura(pair.Key) && HasItemFitToSpellRequirements(spellInfo))
                    AddAura(pair.Key, this); // no SMSG_SPELL_GO in sniff found
            }
        }
        else
            RemoveItemDependentAurasAndCasts(item);
    }

    public void ApplyModToSpell(SpellModifier mod, Spell spell)
    {
        if (spell == null)
            return;

        // don't do anything with no charges
        if (mod.OwnerAura.IsUsingCharges && mod.OwnerAura.Charges == 0)
            return;

        // register inside spell, proc system uses this to drop charges
        spell.AppliedMods.Add(mod.OwnerAura);
    }

    public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref int basevalue, Spell spell = null)
    {
        double val = basevalue;
        ApplySpellMod(spellInfo, op, ref val, spell);
        basevalue = (int)val;
    }

    public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref uint basevalue, Spell spell = null)
    {
        double val = basevalue;
        ApplySpellMod(spellInfo, op, ref val, spell);
        basevalue = (uint)val;
    }

    public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref float basevalue, Spell spell = null)
    {
        double val = basevalue;
        ApplySpellMod(spellInfo, op, ref val, spell);
        basevalue = (float)val;
    }

    public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref double basevalue, Spell spell = null)
    {
        double totalmul = 1.0f;
        double totalflat = 0;

        GetSpellModValues(spellInfo, op, spell, basevalue, ref totalflat, ref totalmul);

        basevalue = (basevalue + totalflat) * totalmul;
    }

    public bool CanNoReagentCast(SpellInfo spellInfo)
    {
        // don't take reagents for spells with SPELL_ATTR5_NO_REAGENT_WHILE_PREP
        if (spellInfo.HasAttribute(SpellAttr5.NoReagentCostWithAura) &&
            HasUnitFlag(UnitFlags.Preparation))
            return true;

        // Check no reagent use mask
        FlagArray128 noReagentMask = new()
        {
            [0] = ActivePlayerData.NoReagentCostMask[0],
            [1] = ActivePlayerData.NoReagentCostMask[1],
            [2] = ActivePlayerData.NoReagentCostMask[2],
            [3] = ActivePlayerData.NoReagentCostMask[3]
        };

        return spellInfo.SpellFamilyFlags & noReagentMask;
    }

    public bool CanSeeSpellClickOn(Creature creature)
    {
        if (!creature.HasNpcFlag(NPCFlags.SpellClick))
            return false;

        var clickBounds = GameObjectManager.GetSpellClickInfoMapBounds(creature.Entry);

        if (clickBounds.Empty())
            return true;

        foreach (var spellClickInfo in clickBounds)
        {
            if (!spellClickInfo.IsFitToRequirements(this, creature))
                return false;

            if (ConditionManager.IsObjectMeetingSpellClickConditions(creature.Entry, spellClickInfo.SpellId, this, creature))
                return true;
        }

        return false;
    }

    public bool CanUseMastery()
    {
        if (CliDB.ChrSpecializationStorage.TryGetValue(GetPrimarySpecialization(), out var chrSpec))
            return HasSpell(chrSpec.MasterySpellID[0]) || HasSpell(chrSpec.MasterySpellID[1]);

        return false;
    }

    public void CastItemCombatSpell(DamageInfo damageInfo)
    {
        var target = damageInfo.Victim;

        if (target == null || !target.IsAlive || target == this)
            return;

        for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
        {
            // If usable, try to cast item spell
            var item = GetItemByPos(InventorySlots.Bag0, i);

            if (item != null)
                if (!item.IsBroken && CanUseAttackType(damageInfo.AttackType))
                {
                    var proto = item.Template;

                    if (proto != null)
                    {
                        // Additional check for weapons
                        if (proto.Class == ItemClass.Weapon)
                        {
                            // offhand item cannot proc from main hand hit etc

                            var slot = damageInfo.AttackType switch
                            {
                                WeaponAttackType.BaseAttack   => EquipmentSlot.MainHand,
                                WeaponAttackType.RangedAttack => EquipmentSlot.MainHand,
                                WeaponAttackType.OffAttack    => EquipmentSlot.OffHand,
                                _                             => EquipmentSlot.End
                            };

                            if (slot != i)
                                continue;

                            // Check if item is useable (forms or disarm)
                            if (damageInfo.AttackType == WeaponAttackType.BaseAttack)
                                if (!IsUseEquipedWeapon(true) && !IsInFeralForm)
                                    continue;
                        }

                        CastItemCombatSpell(damageInfo, item, proto);
                    }
                }
        }
    }

    public void CastItemCombatSpell(DamageInfo damageInfo, Item item, ItemTemplate proto)
    {
        // Can do effect if any damage done to target
        // for done procs allow normal + critical + absorbs by default
        var canTrigger = damageInfo.HitMask.HasAnyFlag(ProcFlagsHit.Normal | ProcFlagsHit.Critical | ProcFlagsHit.Absorb);

        if (canTrigger)
            if (!item.Template.HasFlag(ItemFlags.Legacy))
                foreach (var effectData in item.Effects)
                {
                    // wrong triggering type
                    if (effectData.TriggerType != ItemSpelltriggerType.OnProc)
                        continue;

                    var spellInfo = SpellManager.GetSpellInfo((uint)effectData.SpellID);

                    if (spellInfo == null)
                    {
                        Log.Logger.Error("WORLD: unknown Item spellid {0}", effectData.SpellID);

                        continue;
                    }

                    float chance = spellInfo.ProcChance;

                    if (proto.SpellPPMRate != 0)
                    {
                        var weaponSpeed = GetBaseAttackTime(damageInfo.AttackType);
                        chance = GetPpmProcChance(weaponSpeed, proto.SpellPPMRate, spellInfo);
                    }
                    else if (chance > 100.0f)
                        chance = GetWeaponProcChance();

                    if (RandomHelper.randChance(chance) && ScriptManager.RunScriptRet<IItemOnCastItemCombatSpell>(tmpscript => tmpscript.OnCastItemCombatSpell(this, damageInfo.Victim, spellInfo, item), item.ScriptId))
                        damageInfo.Victim.SpellFactory.CastSpell(item, spellInfo.Id);
                }

        // item combat enchantments
        for (byte eSlot = 0; eSlot < (byte)EnchantmentSlot.Max; ++eSlot)
        {
            var enchantID = item.GetEnchantmentId((EnchantmentSlot)eSlot);

            if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var pEnchant))
                continue;

            for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
            {
                if (pEnchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                    continue;

                var entry = SpellManager.GetSpellEnchantProcEvent(enchantID);

                if (entry != null && entry.HitMask != 0)
                {
                    // Check hit/crit/dodge/parry requirement
                    if ((entry.HitMask & (uint)damageInfo.HitMask) == 0)
                        continue;
                }
                else
                {
                    // for done procs allow normal + critical + absorbs by default
                    if (!canTrigger)
                        continue;
                }

                // check if enchant procs only on white hits
                if (entry != null && entry.AttributesMask.HasAnyFlag(EnchantProcAttributes.WhiteHit) && damageInfo.SpellInfo != null)
                    continue;

                var spellInfo = SpellManager.GetSpellInfo(pEnchant.EffectArg[s]);

                if (spellInfo == null)
                {
                    Log.Logger.Error("Player.CastItemCombatSpell(GUID: {0}, name: {1}, enchant: {2}): unknown spell {3} is casted, ignoring...",
                                     GUID.ToString(),
                                     GetName(),
                                     enchantID,
                                     pEnchant.EffectArg[s]);

                    continue;
                }

                var chance = pEnchant.EffectPointsMin[s] != 0 ? pEnchant.EffectPointsMin[s] : GetWeaponProcChance();

                if (entry != null)
                {
                    if (entry.ProcsPerMinute != 0)
                        chance = GetPpmProcChance(proto.Delay, entry.ProcsPerMinute, spellInfo);
                    else if (entry.Chance != 0)
                        chance = entry.Chance;
                }

                // Apply spell mods
                ApplySpellMod(spellInfo, SpellModOp.ProcChance, ref chance);

                // Shiv has 100% chance to apply the poison
                if (FindCurrentSpellBySpellId(5938) != null && eSlot == (byte)EnchantmentSlot.Temp)
                    chance = 100.0f;

                if (RandomHelper.randChance(chance))
                {
                    if (spellInfo.IsPositive)
                        SpellFactory.CastSpell(item, spellInfo.Id);
                    else
                        damageInfo.Victim.SpellFactory.CastSpell(item, spellInfo.Id);
                }

                if (!RandomHelper.randChance(chance))
                    continue;

                var target = spellInfo.IsPositive ? this : damageInfo.Victim;

                CastSpellExtraArgs args = new(item);

                // reduce effect values if enchant is limited
                if (entry != null && entry.AttributesMask.HasAnyFlag(EnchantProcAttributes.Limit60) && target.GetLevelForTarget(this) > 60)
                {
                    var lvlDifference = (int)target.GetLevelForTarget(this) - 60;
                    var lvlPenaltyFactor = 4; // 4% lost effectiveness per level

                    var effectPct = Math.Max(0, 100 - lvlDifference * lvlPenaltyFactor);

                    foreach (var spellEffectInfo in spellInfo.Effects)
                        if (spellEffectInfo.IsEffect)
                            args.AddSpellMod(SpellValueMod.BasePoint0 + spellEffectInfo.EffectIndex, MathFunctions.CalculatePct(spellEffectInfo.CalcValue(this), effectPct));
                }

                SpellFactory.CastSpell(target, spellInfo.Id, args);
            }
        }
    }

    public void CastItemUseSpell(Item item, SpellCastTargets targets, ObjectGuid castCount, uint[] misc)
    {
        if (!item.Template.HasFlag(ItemFlags.Legacy))
            // item spells casted at use
            foreach (var effectData in item.Effects)
            {
                // wrong triggering type
                if (effectData.TriggerType != ItemSpelltriggerType.OnUse)
                    continue;

                var spellInfo = SpellManager.GetSpellInfo((uint)effectData.SpellID);

                if (spellInfo == null)
                {
                    Log.Logger.Error("Player.CastItemUseSpell: Item (Entry: {0}) in have wrong spell id {1}, ignoring", item.Entry, effectData.SpellID);

                    continue;
                }

                var spell = SpellFactory.NewSpell(spellInfo, TriggerCastFlags.None);

                SpellPrepare spellPrepare = new()
                {
                    ClientCastID = castCount,
                    ServerCastID = spell.CastId
                };

                SendPacket(spellPrepare);

                spell.FromClient = true;
                spell.CastItem = item;
                spell.SpellMisc.Data0 = misc[0];
                spell.SpellMisc.Data1 = misc[1];
                spell.Prepare(targets);

                return;
            }

        // Item enchantments spells casted at use
        for (EnchantmentSlot eSlot = 0; eSlot < EnchantmentSlot.Max; ++eSlot)
        {
            var enchantID = item.GetEnchantmentId(eSlot);

            if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var pEnchant))
                continue;

            for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
            {
                if (pEnchant.Effect[s] != ItemEnchantmentType.UseSpell)
                    continue;

                var spellInfo = SpellManager.GetSpellInfo(pEnchant.EffectArg[s]);

                if (spellInfo == null)
                {
                    Log.Logger.Error("Player.CastItemUseSpell Enchant {0}, cast unknown spell {1}", enchantID, pEnchant.EffectArg[s]);

                    continue;
                }

                var spell = SpellFactory.NewSpell(spellInfo, TriggerCastFlags.None);

                SpellPrepare spellPrepare = new()
                {
                    ClientCastID = castCount,
                    ServerCastID = spell.CastId
                };

                SendPacket(spellPrepare);

                spell.FromClient = true;
                spell.CastItem = item;
                spell.SpellMisc.Data0 = misc[0];
                spell.SpellMisc.Data1 = misc[1];
                spell.Prepare(targets);

                return;
            }
        }
    }

    public override bool CheckAttackFitToAuraRequirement(WeaponAttackType attackType, AuraEffect aurEff)
    {
        var spellInfo = aurEff.SpellInfo;

        if (spellInfo.EquippedItemClass == ItemClass.None)
            return true;

        var item = GetWeaponForAttack(attackType, true);

        if (item == null || !item.IsFitToSpellRequirements(spellInfo))
            return false;

        return true;
    }

    public ushort GetBaseSkillValue(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        int result = skillInfo.SkillRank[skillStatusData.Pos];
        result += skillInfo.SkillPermBonus[skillStatusData.Pos];

        return (ushort)(result < 0 ? 0 : result);
    }

    public override SpellInfo GetCastSpellInfo(SpellInfo spellInfo)
    {
        if (_overrideSpells.TryGetValue(spellInfo.Id, out var overrides))
            foreach (var spellId in overrides)
            {
                var newInfo = SpellManager.GetSpellInfo(spellId, Location.Map.DifficultyID);

                if (newInfo != null)
                    return GetCastSpellInfo(newInfo);
            }

        return base.GetCastSpellInfo(spellInfo);
    }

    public uint GetLastPotionId()
    {
        return _lastPotionId;
    }

    public override SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack)
    {
        var weapon = GetWeaponForAttack(attackType, true);

        if (weapon != null)
            return (SpellSchoolMask)(1 << (int)weapon.Template.DamageType);

        return SpellSchoolMask.Normal;
    }

    public int GetProfessionSlotFor(uint skillId)
    {
        if (!CliDB.SkillLineStorage.TryGetValue(skillId, out var skillEntry))
            return -1;

        if (skillEntry.ParentSkillLineID == 0 || skillEntry.CategoryID != SkillCategory.Profession)
            return -1;

        var slot = 0;

        foreach (var bit in ActivePlayerData.ProfessionSkillLine)
        {
            if (bit == skillId)
                return slot;

            slot++;
        }

        return -1;
    }

    public ushort GetPureMaxSkillValue(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        return skillInfo.SkillMaxRank[skillStatusData.Pos];
    }

    public ushort GetPureSkillValue(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey((uint)skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        return skillInfo.SkillRank[skillStatusData.Pos];
    }

    public uint GetRuneBaseCooldown()
    {
        double cooldown = RuneCooldowns.Base;

        var regenAura = GetAuraEffectsByType(AuraType.ModPowerRegenPercent);

        foreach (var i in regenAura)
            if (i.MiscValue == (int)PowerType.Runes)
                cooldown *= 1.0f - i.Amount / 100.0f;

        // Runes cooldown are now affected by player's haste from equipment ...
        var hastePct = GetRatingBonusValue(CombatRating.HasteMelee);

        // ... and some auras.
        hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste);
        hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste2);
        hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste3);

        cooldown *= 1.0f - hastePct / 100.0f;

        return (uint)cooldown;
    }

    public uint GetRuneCooldown(byte index)
    {
        return _runes.Cooldown[index];
    }

    public byte GetRunesState()
    {
        return (byte)(_runes.RuneState & ((1 << GetMaxPower(PowerType.Runes)) - 1));
    }

    public ushort GetSkillPermBonusValue(uint skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        return skillInfo.SkillPermBonus[skillStatusData.Pos];
    }

    public ushort GetSkillStep(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        return skillInfo.SkillStep[skillStatusData.Pos];
    }

    public ushort GetSkillTempBonusValue(uint skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        return skillInfo.SkillTempBonus[skillStatusData.Pos];
    }

    public ushort GetSkillValue(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        int result = skillInfo.SkillRank[skillStatusData.Pos];
        result += skillInfo.SkillTempBonus[skillStatusData.Pos];
        result += skillInfo.SkillPermBonus[skillStatusData.Pos];

        return (ushort)(result < 0 ? 0 : result);
    }

    public Dictionary<uint, PlayerSpell> GetSpellMap()
    {
        return _spells;
    }

    public void GetSpellModValues<T>(SpellInfo spellInfo, SpellModOp op, Spell spell, T baseValue, ref double flat, ref double pct) where T : IComparable
    {
        flat = 0;
        pct = 1.0f;

        // Drop charges for triggering spells instead of triggered ones
        if (SpellModTakingSpell != null)
            spell = SpellModTakingSpell;

        switch (op)
        {
            // special case, if a mod makes spell instant, only consume that mod
            case SpellModOp.ChangeCastTime:
            {
                SpellModifier modInstantSpell = null;

                foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.Pct])
                {
                    var mod = (SpellModifierByClassMask)spellModifier;

                    if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                        continue;

                    if (baseValue.CompareTo(10000d) >= 0 || !(mod.Value <= -100))
                        continue;

                    modInstantSpell = mod;

                    break;
                }

                if (modInstantSpell == null)
                    foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.LabelPct])
                    {
                        var mod = (SpellPctModifierByLabel)spellModifier;

                        if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                            continue;

                        if (baseValue.CompareTo(10000d) >= 0 || !(mod.Value.ModifierValue <= -1.0f))
                            continue;

                        modInstantSpell = mod;

                        break;
                    }

                if (modInstantSpell != null)
                {
                    ApplyModToSpell(modInstantSpell, spell);
                    pct = 0.0f;

                    return;
                }

                break;
            }
            // special case if two mods apply 100% critical chance, only consume one
            case SpellModOp.CritChance:
            {
                SpellModifier modCritical = null;

                foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.Flat])
                {
                    var mod = (SpellModifierByClassMask)spellModifier;

                    if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                        continue;

                    if (!(mod.Value >= 100))
                        continue;

                    modCritical = mod;

                    break;
                }

                if (modCritical == null)
                    foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.LabelFlat])
                    {
                        var mod = (SpellFlatModifierByLabel)spellModifier;

                        if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                            continue;

                        if (!(mod.Value.ModifierValue >= 100))
                            continue;

                        modCritical = mod;

                        break;
                    }

                if (modCritical != null)
                {
                    ApplyModToSpell(modCritical, spell);
                    flat = 100;

                    return;
                }

                break;
            }
        }

        foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.Flat])
        {
            var mod = (SpellModifierByClassMask)spellModifier;

            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                continue;

            var value = mod.Value;

            if (value == 0)
                continue;

            flat += value;
            ApplyModToSpell(mod, spell);
        }

        foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.LabelFlat])
        {
            var mod = (SpellFlatModifierByLabel)spellModifier;

            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                continue;

            var value = mod.Value.ModifierValue;

            if (value == 0)
                continue;

            flat += value;
            ApplyModToSpell(mod, spell);
        }

        foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.Pct])
        {
            var mod = (SpellModifierByClassMask)spellModifier;

            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                continue;

            // skip percent mods for null basevalue (most important for spell mods with charges)
            if (baseValue + (dynamic)flat == 0)
                continue;

            var value = mod.Value;

            if (value == 0)
                continue;

            // special case (skip > 10sec spell casts for instant cast setting)
            if (op == SpellModOp.ChangeCastTime)
                if (baseValue.CompareTo(10000d) > 0 && value <= -100)
                    continue;

            pct *= 1.0f + MathFunctions.CalculatePct(1.0f, value);
            ApplyModToSpell(mod, spell);
        }

        foreach (var spellModifier in _spellModifiers[(int)op][(int)SpellModType.LabelPct])
        {
            var mod = (SpellPctModifierByLabel)spellModifier;

            if (!IsAffectedBySpellmod(spellInfo, mod, spell))
                continue;

            // skip percent mods for null basevalue (most important for spell mods with charges)
            if (baseValue + (dynamic)flat == 0)
                continue;

            var value = mod.Value.ModifierValue;

            if (value == 1.0f)
                continue;

            // special case (skip > 10sec spell casts for instant cast setting)
            if (op == SpellModOp.ChangeCastTime)
                if (baseValue.CompareTo(10000d) > 0 && value <= -1.0f)
                    continue;

            pct *= value;
            ApplyModToSpell(mod, spell);
        }
    }

    public int GetSpellPenetrationItemMod()
    {
        return _spellPenetrationItemMod;
    }

    public WorldLocation GetStoredAuraTeleportLocation(uint spellId)
    {
        var auraLocation = _storedAuraTeleportLocations.LookupByKey(spellId);

        return auraLocation?.Loc;
    }

    public bool HasActiveSpell(uint spellId)
    {
        if (_spells.TryGetValue(spellId, out var spell))
            return spell.State != PlayerSpellState.Removed && spell.Active && !spell.Disabled;

        return false;
    }

    public bool HasItemFitToSpellRequirements(SpellInfo spellInfo, Item ignoreItem = null)
    {
        if (spellInfo.EquippedItemClass < 0)
            return true;

        // scan other equipped items for same requirements (mostly 2 daggers/etc)
        // for optimize check 2 used cases only
        switch (spellInfo.EquippedItemClass)
        {
            case ItemClass.Weapon:
            {
                var item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                if (item != null)
                    if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                        return true;

                item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                if (item != null)
                    if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                        return true;

                break;
            }
            case ItemClass.Armor:
            {
                if (!spellInfo.HasAttribute(SpellAttr8.ArmorSpecialization))
                {
                    // most used check: shield only
                    if ((spellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassArmor.Shield)) != 0)
                    {
                        var item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                        if (item != null)
                            if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                                return true;

                        // special check to filter things like Shield Wall, the aura is not permanent and must stay even without required item
                        if (!spellInfo.IsPassive)
                            foreach (var spellEffectInfo in spellInfo.Effects)
                                if (spellEffectInfo.IsAura)
                                    return true;
                    }

                    // tabard not have dependent spells
                    for (var i = EquipmentSlot.Start; i < EquipmentSlot.MainHand; ++i)
                    {
                        var item = GetUseableItemByPos(InventorySlots.Bag0, i);

                        if (item == null)
                            continue;

                        if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
                            return true;
                    }
                }
                else
                {
                    // requires item equipped in all armor slots
                    foreach (var i in new[]
                             {
                                 EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Chest, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Wrist, EquipmentSlot.Hands
                             })
                    {
                        var item = GetUseableItemByPos(InventorySlots.Bag0, i);

                        if (item == null || item == ignoreItem || !item.IsFitToSpellRequirements(spellInfo))
                            return false;
                    }

                    return true;
                }

                break;
            }
            default:
                Log.Logger.Error("HasItemFitToSpellRequirements: Not handled spell requirement for item class {0}", spellInfo.EquippedItemClass);

                break;
        }

        return false;
    }

    public bool HasSkill(SkillType skill)
    {
        return HasSkill((uint)skill);
    }

    public bool HasSkill(uint skill)
    {
        if (skill == 0)
            return false;

        SkillInfo skillInfoField = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        return skillStatusData != null && skillStatusData.State != SkillState.Deleted && skillInfoField.SkillRank[skillStatusData.Pos] != 0;
    }

    public override bool HasSpell(uint spellId)
    {
        if (_spells.TryGetValue(spellId, out var spell))
            return spell.State != PlayerSpellState.Removed && !spell.Disabled;

        return false;
    }

    public void InitRunes()
    {
        if (Class != PlayerClass.Deathknight)
            return;

        var runeIndex = GetPowerIndex(PowerType.Runes);

        if (runeIndex == (int)PowerType.Max)
            return;

        _runes = new Runes
        {
            RuneState = 0
        };

        for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
            SetRuneCooldown(i, 0); // reset cooldowns

        // set a base regen timer equal to 10 sec
        SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenFlatModifier, (int)runeIndex), 0.0f);
        SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenInterruptedFlatModifier, (int)runeIndex), 0.0f);
    }

    public void LearnCustomSpells()
    {
        //if (!WorldConfig.GetBoolValue(WorldCfg.StartAllSpells)) // this is not all spells, just custom ones.
        //    return;

        // learn default race/class spells
        var info = GameObjectManager.GetPlayerInfo(Race, Class);

        foreach (var tspell in info.CustomSpells)
        {
            Log.Logger.Debug("PLAYER (Class: {0} Race: {1}): Adding initial spell, id = {2}", Class, Race, tspell);

            if (!Location.IsInWorld) // will send in INITIAL_SPELLS in list anyway at map add
                AddSpell(tspell, true, true, true, false);
            else // but send in normal spell in GameInfo learn case
                LearnSpell(tspell, true);
        }
    }

    public void LearnDefaultSkill(SkillRaceClassInfoRecord rcInfo)
    {
        var skillId = (SkillType)rcInfo.SkillID;

        switch (SpellManager.GetSkillRangeType(rcInfo))
        {
            case SkillRangeType.Language:
                SetSkill(skillId, 0, 300, 300);

                break;

            case SkillRangeType.Level:
            {
                ushort skillValue = 1;
                var maxValue = GetMaxSkillValueForLevel();

                if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                    skillValue = maxValue;
                else if (Class == PlayerClass.Deathknight)
                    skillValue = (ushort)Math.Min(Math.Max(1, (Level - 1) * 5), maxValue);

                SetSkill(skillId, 0, skillValue, maxValue);

                break;
            }
            case SkillRangeType.Mono:
                SetSkill(skillId, 0, 1, 1);

                break;

            case SkillRangeType.Rank:
            {
                var tier = GameObjectManager.GetSkillTier(rcInfo.SkillTierID);
                var maxValue = (ushort)tier.Value[0];
                ushort skillValue = 1;

                if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                    skillValue = maxValue;
                else if (Class == PlayerClass.Deathknight)
                    skillValue = (ushort)Math.Min(Math.Max(1, (Level - 1) * 5), maxValue);

                SetSkill(skillId, 1, skillValue, maxValue);

                break;
            }
        }
    }

    public void LearnDefaultSkills()
    {
        // learn default race/class skills
        var info = GameObjectManager.GetPlayerInfo(Race, Class);

        foreach (var rcInfo in info.Skills)
        {
            if (HasSkill((SkillType)rcInfo.SkillID))
                continue;

            if (rcInfo.MinLevel > Level)
                continue;

            LearnDefaultSkill(rcInfo);
        }
    }

    public void LearnSkillRewardedSpells(uint skillId, uint skillValue, Race race)
    {
        var raceMask = SharedConst.GetMaskForRace(race);
        var classMask = ClassMask;

        var skillLineAbilities = DB2Manager.GetSkillLineAbilitiesBySkill(skillId);

        foreach (var ability in skillLineAbilities)
        {
            if (ability.SkillLine != skillId)
                continue;

            var spellInfo = SpellManager.GetSpellInfo(ability.Spell);

            if (spellInfo == null)
                continue;

            switch (ability.AcquireMethod)
            {
                case AbilityLearnType.OnSkillValue:
                case AbilityLearnType.OnSkillLearn:
                    break;

                case AbilityLearnType.RewardedFromQuest:
                    if (!ability.Flags.HasAnyFlag(SkillLineAbilityFlags.CanFallbackToLearnedOnSkillLearn) ||
                        !spellInfo.MeetsFutureSpellPlayerCondition(this))
                        continue;

                    break;

                default:
                    continue;
            }

            // AcquireMethod == 2 && NumSkillUps == 1 -. automatically learn riding skill spell, else we skip it (client shows riding in spellbook as trainable).
            if (skillId == (uint)SkillType.Riding && (ability.AcquireMethod != AbilityLearnType.OnSkillLearn || ability.NumSkillUps != 1))
                continue;

            // Check race if set
            if (ability.RaceMask != 0 && !Convert.ToBoolean(ability.RaceMask & raceMask))
                continue;

            // Check class if set
            if (ability.ClassMask != 0 && !Convert.ToBoolean(ability.ClassMask & classMask))
                continue;

            // check level, skip class spells if not high enough
            if (Level < spellInfo.SpellLevel)
                continue;

            // need unlearn spell
            if (skillValue < ability.MinSkillLineRank && ability.AcquireMethod == AbilityLearnType.OnSkillValue)
                RemoveSpell(ability.Spell);
            // need learn
            else if (!Location.IsInWorld)
                AddSpell(ability.Spell, true, true, true, false, false, ability.SkillLine);
            else
                LearnSpell(ability.Spell, true, ability.SkillLine);
        }
    }

    public void LearnSpecializationSpells()
    {
        var specSpells = DB2Manager.GetSpecializationSpells(GetPrimarySpecialization());

        if (specSpells != null)
            for (var j = 0; j < specSpells.Count; ++j)
            {
                var specSpell = specSpells[j];
                var spellInfo = SpellManager.GetSpellInfo(specSpell.SpellID);

                if (spellInfo == null || spellInfo.SpellLevel > Level)
                    continue;

                LearnSpell(specSpell.SpellID, true);

                if (specSpell.OverridesSpellID != 0)
                    AddOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
            }
    }

    public void LearnSpell<T>(T spellId, bool dependent, uint fromSkill = 0, bool suppressMessaging = false, int? traitDefinitionId = null) where T : struct, Enum
    {
        LearnSpell(Convert.ToUInt32(spellId), dependent, fromSkill, suppressMessaging, traitDefinitionId);
    }

    public void LearnSpell(uint spellId, bool dependent, uint fromSkill = 0, bool suppressMessaging = false, int? traitDefinitionId = null)
    {
        var playerSpell = _spells.LookupByKey(spellId);

        var disabled = playerSpell is { Disabled: true };
        var active = !disabled || playerSpell.Active;
        var favorite = playerSpell?.Favorite ?? false;

        var learning = AddSpell(spellId, active, true, dependent, false, false, fromSkill, favorite, traitDefinitionId);

        // prevent duplicated entires in spell book, also not send if not in world (loading)
        if (learning && Location.IsInWorld)
        {
            LearnedSpells learnedSpells = new();

            LearnedSpellInfo learnedSpellInfo = new()
            {
                SpellID = spellId,
                IsFavorite = favorite,
                TraitDefinitionID = traitDefinitionId
            };

            learnedSpells.SuppressMessaging = suppressMessaging;
            learnedSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
            SendPacket(learnedSpells);
        }

        // learn all disabled higher ranks and required spells (recursive)
        if (disabled)
        {
            var nextSpell = SpellManager.GetNextSpellInChain(spellId);

            if (nextSpell != 0)
            {
                var spell = _spells.LookupByKey(nextSpell);

                if (spellId != 0 && spell.Disabled)
                    LearnSpell(nextSpell, false, fromSkill);
            }

            var spellsRequiringSpell = SpellManager.GetSpellsRequiringSpellBounds(spellId);

            foreach (var id in spellsRequiringSpell)
            {
                var spell1 = _spells.LookupByKey(id);

                if (spell1 is { Disabled: true })
                    LearnSpell(id, false, fromSkill);
            }
        }
        else
            UpdateQuestObjectiveProgress(QuestObjectiveType.LearnSpell, (int)spellId, 1);
    }

    public void ModifySkillBonus(SkillType skillid, int val, bool talent)
    {
        ModifySkillBonus((uint)skillid, val, talent);
    }

    public void ModifySkillBonus(uint skillid, int val, bool talent)
    {
        SkillInfo skillInfoField = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skillid);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfoField.SkillRank[skillStatusData.Pos] == 0)
            return;

        if (talent)
            SetSkillPermBonus(skillStatusData.Pos, (ushort)(skillInfoField.SkillPermBonus[skillStatusData.Pos] + val));
        else
            SetSkillTempBonus(skillStatusData.Pos, (ushort)(skillInfoField.SkillTempBonus[skillStatusData.Pos] + val));

        // Apply/Remove bonus to child skill lines
        var childSkillLines = DB2Manager.GetSkillLinesForParentSkill(skillid);

        if (childSkillLines != null)
            foreach (var childSkillLine in childSkillLines)
                ModifySkillBonus(childSkillLine.Id, val, talent);
    }

    public void PetSpellInitialize()
    {
        var pet = CurrentPet;

        if (pet == null)
            return;

        Log.Logger.Debug("Pet Spells Groups");

        var charmInfo = pet.GetCharmInfo();

        PetSpells petSpellsPacket = new()
        {
            PetGUID = pet.GUID,
            CreatureFamily = (ushort)pet.Template.Family, // creature family (required for pet talents)
            Specialization = pet.Specialization,
            TimeLimit = (uint)pet.Duration,
            ReactState = pet.ReactState,
            CommandState = charmInfo.CommandState
        };

        // action bar loop
        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            petSpellsPacket.ActionButtons[i] = charmInfo.GetActionBarEntry(i).PackedData;

        if (pet.IsPermanentPetFor(this))
            // spells loop
            foreach (var pair in pet.Spells)
            {
                if (pair.Value.State == PetSpellState.Removed)
                    continue;

                petSpellsPacket.Actions.Add(UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(pair.Key, (uint)pair.Value.Active));
            }

        // Cooldowns
        pet.
            // Cooldowns
            SpellHistory.WritePacket(petSpellsPacket);

        SendPacket(petSpellsPacket);
    }

    public void RemoveArenaEnchantments(EnchantmentSlot slot)
    {
        // remove enchantments from equipped items first to clean up the m_enchantDuration list
        for (var i = 0; i < _enchantDurations.Count; ++i)
        {
            var enchantDuration = _enchantDurations[i];

            if (enchantDuration.Slot != slot)
                continue;

            if (enchantDuration.Item != null && enchantDuration.Item.GetEnchantmentId(slot) != 0)
            {
                // Poisons and DK runes are enchants which are allowed on arenas
                if (SpellManager.IsArenaAllowedEnchancment(enchantDuration.Item.GetEnchantmentId(slot)))
                    continue;

                // remove from stats
                ApplyEnchantment(enchantDuration.Item, slot, false, false);
                // remove visual
                enchantDuration.Item.ClearEnchantment(slot);
            }

            // remove from update list
            _enchantDurations.Remove(enchantDuration);
        }

        // remove enchants from inventory items
        // NOTE: no need to remove these from stats, since these aren't equipped
        // in inventory
        var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

        for (var i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
        {
            var pItem = GetItemByPos(InventorySlots.Bag0, i);

            if (pItem != null && !SpellManager.IsArenaAllowedEnchancment(pItem.GetEnchantmentId(slot)))
                pItem.ClearEnchantment(slot);
        }

        // in inventory bags
        for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
        {
            var pBag = GetBagByPos(i);

            if (pBag == null)
                continue;

            for (byte j = 0; j < pBag.GetBagSize(); j++)
            {
                var pItem = pBag.GetItemByPos(j);

                if (pItem != null && !SpellManager.IsArenaAllowedEnchancment(pItem.GetEnchantmentId(slot)))
                    pItem.ClearEnchantment(slot);
            }
        }
    }

    public void RemoveArenaSpellCooldowns(bool removeActivePetCooldowns)
    {
        // remove cooldowns on spells that have < 10 min CD
        SpellHistory
            .ResetCooldowns(p =>
                            {
                                var spellInfo = SpellManager.GetSpellInfo(p.Key);

                                return spellInfo.RecoveryTime < 10 * Time.MINUTE * Time.IN_MILLISECONDS && spellInfo.CategoryRecoveryTime < 10 * Time.MINUTE * Time.IN_MILLISECONDS && !spellInfo.HasAttribute(SpellAttr6.DoNotResetCooldownInArena);
                            },
                            true);

        // pet cooldowns
        if (removeActivePetCooldowns)
        {
                CurrentPet?.SpellHistory.ResetAllCooldowns();
        }
    }

    public void RemoveOverrideSpell(uint overridenSpellId, uint newSpellId)
    {
        _overrideSpells.Remove(overridenSpellId, newSpellId);
    }

    public void RemoveSpell<T>(T spellId, bool disabled = false, bool learnLowRank = true, bool suppressMessaging = false) where T : struct, Enum
    {
        RemoveSpell(Convert.ToUInt32(spellId), disabled, learnLowRank, suppressMessaging);
    }

    public void RemoveSpell(uint spellId, bool disabled = false, bool learnLowRank = true, bool suppressMessaging = false)
    {
        if (!_spells.TryGetValue(spellId, out var pSpell))
            return;

        if (pSpell.State == PlayerSpellState.Removed || (disabled && pSpell.Disabled) || pSpell.State == PlayerSpellState.Temporary)
            return;

        // unlearn non talent higher ranks (recursive)
        var nextSpell = SpellManager.GetNextSpellInChain(spellId);

        if (nextSpell != 0)
        {
            var spellInfo1 = SpellManager.GetSpellInfo(nextSpell);

            if (HasSpell(nextSpell) && !spellInfo1.HasAttribute(SpellCustomAttributes.IsTalent))
                RemoveSpell(nextSpell, disabled, false);
        }

        //unlearn spells dependent from recently removed spells
        var spellsRequiringSpell = SpellManager.GetSpellsRequiringSpellBounds(spellId);

        foreach (var id in spellsRequiringSpell)
            RemoveSpell(id, disabled);

        // re-search, it can be corrupted in prev loop
        pSpell = _spells.LookupByKey(spellId);

        if (pSpell == null)
            return; // already unleared

        var curActive = pSpell.Active;
        var curDependent = pSpell.Dependent;

        if (disabled)
        {
            pSpell.Disabled = true;

            if (pSpell.State != PlayerSpellState.New)
                pSpell.State = PlayerSpellState.Changed;
        }
        else
        {
            if (pSpell.State == PlayerSpellState.New)
                _spells.Remove(spellId);
            else
                pSpell.State = PlayerSpellState.Removed;
        }

        RemoveOwnedAura(spellId, GUID);

        // remove pet auras
        var valueCollection = SpellManager.GetPetAuras(spellId)?.Values;

        if (valueCollection != null)
            foreach (var petAur in valueCollection)
                RemovePetAura(petAur);

        // update free primary prof.points (if not overflow setting, can be in case GM use before .learn prof. learning)
        var spellInfo = SpellManager.GetSpellInfo(spellId);

        if (spellInfo is { IsPrimaryProfessionFirstRank: true })
        {
            var freeProfs = FreePrimaryProfessionPoints + 1;

            if (freeProfs <= Configuration.GetDefaultValue("MaxPrimaryTradeSkill", 2))
                SetFreePrimaryProfessions(freeProfs);
        }

        // remove dependent skill
        var spellLearnSkill = SpellManager.GetSpellLearnSkill(spellId);

        if (spellLearnSkill != null)
        {
            var prevSpell = SpellManager.GetPrevSpellInChain(spellId);

            if (prevSpell == 0) // first rank, remove skill
                SetSkill(spellLearnSkill.Skill, 0, 0, 0);
            else
            {
                // search prev. skill setting by spell ranks chain
                var prevSkill = SpellManager.GetSpellLearnSkill(prevSpell);

                while (prevSkill == null && prevSpell != 0)
                {
                    prevSpell = SpellManager.GetPrevSpellInChain(prevSpell);
                    prevSkill = SpellManager.GetSpellLearnSkill(SpellManager.GetFirstSpellInChain(prevSpell));
                }

                if (prevSkill == null) // not found prev skill setting, remove skill
                    SetSkill(spellLearnSkill.Skill, 0, 0, 0);
                else // set to prev. skill setting values
                {
                    uint skillValue = GetPureSkillValue(prevSkill.Skill);
                    uint skillMaxValue = GetPureMaxSkillValue(prevSkill.Skill);

                    if (skillValue > prevSkill.Value)
                        skillValue = prevSkill.Value;

                    uint newSkillMaxValue = prevSkill.Maxvalue == 0 ? GetMaxSkillValueForLevel() : prevSkill.Maxvalue;

                    if (skillMaxValue > newSkillMaxValue)
                        skillMaxValue = newSkillMaxValue;

                    SetSkill(prevSkill.Skill, prevSkill.Step, skillValue, skillMaxValue);
                }
            }
        }

        // remove dependent spells
        var spellBounds = SpellManager.GetSpellLearnSpellMapBounds(spellId);

        foreach (var spellNode in spellBounds)
        {
            RemoveSpell(spellNode.Spell, disabled);

            if (spellNode.OverridesSpell != 0)
                RemoveOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
        }

        // activate lesser rank in spellbook/action bar, and cast it if need
        var prevActivate = false;

        var prevID = SpellManager.GetPrevSpellInChain(spellId);

        if (prevID != 0)
            // if ranked non-stackable spell: need activate lesser rank and update dendence state
            // No need to check for spellInfo != NULL here because if cur_active is true, then that means that the spell was already in m_spells, and only valid spells can be pushed there.
            if (curActive && spellInfo.IsRanked)
                // need manually update dependence state (learn spell ignore like attempts)
                if (_spells.TryGetValue(prevID, out var prevSpell))
                {
                    if (prevSpell.Dependent != curDependent)
                    {
                        prevSpell.Dependent = curDependent;

                        if (prevSpell.State != PlayerSpellState.New)
                            prevSpell.State = PlayerSpellState.Changed;
                    }

                    // now re-learn if need re-activate
                    if (!prevSpell.Active && learnLowRank)
                        if (AddSpell(prevID, true, false, prevSpell.Dependent, prevSpell.Disabled))
                        {
                            // downgrade spell ranks in spellbook and action bar
                            SendSupercededSpell(spellId, prevID);
                            prevActivate = true;
                        }
                }

        _overrideSpells.Remove(spellId);

        if (_canTitanGrip)
            if (spellInfo is { IsPassive: true } && spellInfo.HasEffect(SpellEffectName.TitanGrip))
            {
                RemoveAura(_titanGripPenaltySpellId);
                SetCanTitanGrip(false);
            }

        if (CanDualWield)
            if (spellInfo is { IsPassive: true } && spellInfo.HasEffect(SpellEffectName.DualWield))
                SetCanDualWield(false);

        if (Configuration.GetDefaultValue("OffhandCheckAtSpellUnlearn", true))
            AutoUnequipOffhandIfNeed();

        // remove from spell book if not replaced by lesser rank
        if (!prevActivate)
        {
            UnlearnedSpells unlearnedSpells = new();
            unlearnedSpells.SpellID.Add(spellId);
            unlearnedSpells.SuppressMessaging = suppressMessaging;
            SendPacket(unlearnedSpells);
        }
    }

    public void RemoveStoredAuraTeleportLocation(uint spellId)
    {
        if (_storedAuraTeleportLocations.TryGetValue(spellId, out var storedLocation))
            storedLocation.CurrentState = StoredAuraTeleportLocation.State.Deleted;
    }

    public void RemoveTemporarySpell(uint spellId)
    {
        var spell = _spells.LookupByKey(spellId);

        // spell already not in list - do not do anything
        if (spell is not { State: PlayerSpellState.Temporary })
            return;

        // spell has other state than temporary - do not change it

        _spells.Remove(spellId);
    }

    public void ResyncRunes()
    {
        var maxRunes = GetMaxPower(PowerType.Runes);

        ResyncRunes data = new()
        {
            Runes =
            {
                Start = (byte)((1 << maxRunes) - 1),
                Count = GetRunesState()
            }
        };

        float baseCd = GetRuneBaseCooldown();

        for (byte i = 0; i < maxRunes; ++i)
            data.Runes.Cooldowns.Add((byte)((baseCd - GetRuneCooldown(i)) / baseCd * 255));

        SendPacket(data);
    }

    public void SendSpellCategoryCooldowns()
    {
        SpellCategoryCooldown cooldowns = new();

        var categoryCooldownAuras = GetAuraEffectsByType(AuraType.ModSpellCategoryCooldown);

        foreach (var aurEff in categoryCooldownAuras)
        {
            var categoryId = (uint)aurEff.MiscValue;
            var cooldownInfo = cooldowns.CategoryCooldowns.Find(p => p.Category == categoryId);

            if (cooldownInfo == null)
                cooldowns.CategoryCooldowns.Add(new SpellCategoryCooldown.CategoryCooldownInfo(categoryId, -(int)aurEff.Amount));
            else
                cooldownInfo.ModCooldown -= (int)aurEff.Amount;
        }

        SendPacket(cooldowns);
    }

    public void SetLastPotionId(uint itemID)
    {
        _lastPotionId = itemID;
    }

    public void SetNoRegentCostMask(FlagArray128 mask)
    {
        for (byte i = 0; i < 4; ++i)
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NoReagentCostMask, i), mask[i]);
    }

    public void SetOverrideSpellsId(uint overrideSpellsId)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideSpellsID), overrideSpellsId);
    }

    public void SetPetSpellPower(uint spellPower)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.PetSpellPower), spellPower);
    }

    public void SetRuneCooldown(byte index, uint cooldown)
    {
        _runes.Cooldown[index] = cooldown;
        _runes.SetRuneState(index, cooldown == 0);
        var activeRunes = _runes.Cooldown.Count(p => p == 0);

        if (activeRunes != GetPower(PowerType.Runes))
            SetPower(PowerType.Runes, activeRunes);
    }

    public void SetSkill(SkillType skill, uint step, uint newVal, uint maxVal)
    {
        SetSkill((uint)skill, step, newVal, maxVal);
    }

    public void SetSkill(uint id, uint step, uint newVal, uint maxVal)
    {
        if (!CliDB.SkillLineStorage.TryGetValue(id, out var skillEntry))
        {
            Log.Logger.Error($"Player.Spells.SetSkill: Skillid: {id} not found in SkillLineStorage for player {GetName()} ({GUID})");

            return;
        }

        ushort currVal;
        var skillStatusData = _skillStatus.LookupByKey(id);
        SkillInfo skillInfoField = ActivePlayerData.Skill;

        void RefreshSkillBonusAuras()
        {
            // Temporary bonuses
            foreach (var effect in GetAuraEffectsByType(AuraType.ModSkill))
                if (effect.MiscValue == id)
                    effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);

            foreach (var effect in GetAuraEffectsByType(AuraType.ModSkill2))
                if (effect.MiscValue == id)
                    effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);

            // Permanent bonuses
            foreach (var effect in GetAuraEffectsByType(AuraType.ModSkillTalent))
                if (effect.MiscValue == id)
                    effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);
        }

        // Handle already stored skills
        if (skillStatusData != null)
        {
            currVal = skillInfoField.SkillRank[skillStatusData.Pos];

            // Activate and update skill line
            if (newVal != 0)
            {
                // if skill value is going down, update enchantments before setting the new value
                if (newVal < currVal)
                    UpdateSkillEnchantments(id, currVal, (ushort)newVal);

                // update step
                SetSkillStep(skillStatusData.Pos, (ushort)step);
                // update value
                SetSkillRank(skillStatusData.Pos, (ushort)newVal);
                SetSkillMaxRank(skillStatusData.Pos, (ushort)maxVal);

                LearnSkillRewardedSpells(id, newVal, Race);

                // if skill value is going up, update enchantments after setting the new value
                if (newVal > currVal)
                {
                    UpdateSkillEnchantments(id, currVal, (ushort)newVal);

                    if (id == (uint)SkillType.Riding)
                        UpdateMountCapability();
                }

                UpdateCriteria(CriteriaType.SkillRaised, id);
                UpdateCriteria(CriteriaType.AchieveSkillStep, id);

                // update skill state
                if (skillStatusData.State is SkillState.Unchanged or SkillState.Deleted)
                {
                    if (currVal == 0) // activated skill, mark as new to save into database
                    {
                        skillStatusData.State = SkillState.New;

                        // Set profession line
                        var freeProfessionSlot = FindEmptyProfessionSlotFor(id);

                        if (freeProfessionSlot != -1)
                            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, freeProfessionSlot), id);

                        RefreshSkillBonusAuras();
                    }
                    else // updated skill, mark as changed to save into database
                        skillStatusData.State = SkillState.Changed;
                }
            }
            else if (currVal != 0) // Deactivate skill line
            {
                // Try to store profession tools and accessories into the bag
                // If we can't, we can't unlearn the profession
                var professionSlot = GetProfessionSlotFor(id);

                if (professionSlot != -1)
                {
                    var professionSlotStart = (byte)(ProfessionSlots.Profession1Tool + professionSlot * ProfessionSlots.MaxCount);

                    // Get all profession items equipped
                    for (byte slotOffset = 0; slotOffset < ProfessionSlots.MaxCount; ++slotOffset)
                    {
                        var professionItem = GetItemByPos(InventorySlots.Bag0, (byte)(professionSlotStart + slotOffset));

                        if (professionItem != null)
                        {
                            // Store item in bag
                            List<ItemPosCount> professionItemDest = new();

                            if (CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, professionItemDest, professionItem) != InventoryResult.Ok)
                            {
                                SendPacket(new DisplayGameError(GameError.InvFull));

                                return;
                            }

                            RemoveItem(InventorySlots.Bag0, professionItem.Slot, true);
                            StoreItem(professionItemDest, professionItem, true);
                        }
                    }

                    // Clear profession lines
                    SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, professionSlot), 0u);
                }

                //remove enchantments needing this skill
                UpdateSkillEnchantments(id, currVal, 0);
                // clear skill fields
                SetSkillStep(skillStatusData.Pos, 0);
                SetSkillRank(skillStatusData.Pos, 0);
                SetSkillStartingRank(skillStatusData.Pos, 1);
                SetSkillMaxRank(skillStatusData.Pos, 0);
                SetSkillTempBonus(skillStatusData.Pos, 0);
                SetSkillPermBonus(skillStatusData.Pos, 0);

                // mark as deleted so the next save will delete the data from the database
                skillStatusData.State = SkillState.Deleted;

                // remove all spells that related to this skill
                var skillLineAbilities = DB2Manager.GetSkillLineAbilitiesBySkill(id);

                foreach (var skillLineAbility in skillLineAbilities)
                    RemoveSpell(SpellManager.GetFirstSpellInChain(skillLineAbility.Spell));

                var childSkillLines = DB2Manager.GetSkillLinesForParentSkill(id);

                if (childSkillLines == null)
                    return;

                foreach (var childSkillLine in childSkillLines.Where(childSkillLine => childSkillLine.ParentSkillLineID == id))
                    SetSkill(childSkillLine.Id, 0, 0, 0);
            }
        }
        else
        {
            // We are about to learn a skill that has been added outside of normal circumstances (Game Master command, scripts etc.)
            byte skillSlot = 0;

            // Find a free skill slot
            for (var i = 0; i < SkillConst.MaxPlayerSkills; ++i)
                if (((SkillInfo)ActivePlayerData.Skill).SkillLineID[i] == 0)
                {
                    skillSlot = (byte)i;

                    break;
                }

            if (skillSlot == 0)
            {
                Log.Logger.Error($"Tried to add skill {id} but player {GetName()} ({GUID}) cannot have additional skills");

                return;
            }

            if (skillEntry.ParentSkillLineID != 0)
            {
                if (skillEntry.ParentTierIndex > 0)
                {
                    var rcEntry = DB2Manager.GetSkillRaceClassInfo(skillEntry.ParentSkillLineID, Race, Class);

                    if (rcEntry != null)
                    {
                        var tier = GameObjectManager.GetSkillTier(rcEntry.SkillTierID);

                        if (tier != null)
                        {
                            var skillval = GetPureSkillValue((SkillType)skillEntry.ParentSkillLineID);
                            SetSkill(skillEntry.ParentSkillLineID, (uint)skillEntry.ParentTierIndex, Math.Max(skillval, (ushort)1), tier.Value[skillEntry.ParentTierIndex - 1]);
                        }
                    }
                }
            }
            else
            {
                // also learn missing child skills at 0 value
                var childSkillLines = DB2Manager.GetSkillLinesForParentSkill(id);

                if (childSkillLines != null)
                    foreach (var childSkillLine in childSkillLines)
                        if (!HasSkill((SkillType)childSkillLine.Id))
                            SetSkill(childSkillLine.Id, 0, 0, 0);

                var freeProfessionSlot = FindEmptyProfessionSlotFor(id);

                if (freeProfessionSlot != -1)
                    SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, freeProfessionSlot), id);
            }

            SetSkillLineId(skillSlot, (ushort)id);

            SetSkillStep(skillSlot, (ushort)step);
            SetSkillRank(skillSlot, (ushort)newVal);
            SetSkillStartingRank(skillSlot, 1);
            SetSkillMaxRank(skillSlot, (ushort)maxVal);

            // apply skill bonuses
            SetSkillTempBonus(skillSlot, 0);
            SetSkillPermBonus(skillSlot, 0);

            UpdateSkillEnchantments(id, 0, (ushort)newVal);

            _skillStatus.Add(id, new SkillStatusData(skillSlot, SkillState.New));

            if (newVal != 0)
            {
                RefreshSkillBonusAuras();

                // Learn all spells for skill
                LearnSkillRewardedSpells(id, newVal, Race);
                UpdateCriteria(CriteriaType.SkillRaised, id);
                UpdateCriteria(CriteriaType.AchieveSkillStep, id);
            }
        }
    }

    public void SetSkillLineId(uint pos, ushort skillLineId)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillLineID, (int)pos), skillLineId);
    }

    public void SetSkillMaxRank(uint pos, ushort max)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillMaxRank, (int)pos), max);
    }

    public void SetSkillPermBonus(uint pos, ushort bonus)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillPermBonus, (int)pos), bonus);
    }

    public void SetSkillRank(uint pos, ushort rank)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillRank, (int)pos), rank);
    }

    public void SetSkillStartingRank(uint pos, ushort starting)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillStartingRank, (int)pos), starting);
    }

    public void SetSkillStep(uint pos, ushort step)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillStep, (int)pos), step);
    }

    public void SetSkillTempBonus(uint pos, ushort bonus)
    {
        SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
        SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillTempBonus, (int)pos), bonus);
    }

    public void SetSpellFavorite(uint spellId, bool favorite)
    {
        if (!_spells.TryGetValue(spellId, out var spell))
            return;

        spell.Favorite = favorite;

        if (spell.State == PlayerSpellState.Unchanged)
            spell.State = PlayerSpellState.Changed;
    }

    public void SetSpellModTakingSpell(Spell spell, bool apply)
    {
        if (apply && SpellModTakingSpell != null)
            return;

        if (!apply && (SpellModTakingSpell == null || SpellModTakingSpell != spell))
            return;

        SpellModTakingSpell = apply ? spell : null;
    }

    public void StopCastingBindSight()
    {
        var target = Viewpoint;

        if (target == null)
            return;

        if (!target.IsTypeMask(TypeMask.Unit))
            return;

        ((Unit)target).RemoveAurasByType(AuraType.BindSight, GUID);
        ((Unit)target).RemoveAurasByType(AuraType.ModPossess, GUID);
        ((Unit)target).RemoveAurasByType(AuraType.ModPossessPet, GUID);
    }

    public void UpdateAllRunesRegen()
    {
        if (Class != PlayerClass.Deathknight)
            return;

        var runeIndex = GetPowerIndex(PowerType.Runes);

        if (runeIndex == (int)PowerType.Max)
            return;

        var runeEntry = DB2Manager.GetPowerTypeEntry(PowerType.Runes);

        var cooldown = GetRuneBaseCooldown();
        SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenFlatModifier, (int)runeIndex), (float)(1 * Time.IN_MILLISECONDS) / cooldown - runeEntry.RegenPeace);
        SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenInterruptedFlatModifier, (int)runeIndex), (float)(1 * Time.IN_MILLISECONDS) / cooldown - runeEntry.RegenCombat);
    }

    public void UpdateAllWeaponDependentCritAuras()
    {
        for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
            UpdateWeaponDependentCritAuras(attackType);
    }

    public void UpdateAreaDependentAuras(uint newArea)
    {
        // remove auras from spells with area limitations
        // use m_zoneUpdateId for speed: UpdateArea called from UpdateZone or instead UpdateZone in both cases m_zoneUpdateId up-to-date
        OwnedAurasList
            .CallOnMatch(aura => aura.SpellInfo.CheckLocation(Location.MapId, _zoneUpdateId, newArea, this) != SpellCastResult.SpellCastOk,
                         pair => RemoveOwnedAura(pair.SpellInfo.Id, pair));

        // some auras applied at subzone enter
        var saBounds = SpellManager.GetSpellAreaForAreaMapBounds(newArea);

        foreach (var spell in saBounds)
            if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, _zoneUpdateId, newArea))
                if (!HasAura(spell.SpellId))
                    SpellFactory.CastSpell(this, spell.SpellId, true);
    }

    public bool UpdateCraftSkill(SpellInfo spellInfo)
    {
        if (spellInfo.HasAttribute(SpellAttr1.NoSkillIncrease))
            return false;

        Log.Logger.Debug("UpdateCraftSkill spellid {0}", spellInfo.Id);

        var bounds = SpellManager.GetSkillLineAbilityMapBounds(spellInfo.Id);

        foreach (var spellIdx in bounds)
            if (spellIdx.SkillupSkillLineID != 0)
            {
                uint skillValue = GetPureSkillValue((SkillType)spellIdx.SkillupSkillLineID);

                // Alchemy Discoveries here
                if (spellInfo.Mechanic == Mechanics.Discovery)
                {
                    var discoveredSpell = SkillDiscovery.GetSkillDiscoverySpell(spellIdx.SkillupSkillLineID, spellInfo.Id, this);

                    if (discoveredSpell != 0)
                        LearnSpell(discoveredSpell, false);
                }

                var craftSkillGain = spellIdx.NumSkillUps * Configuration.GetDefaultValue("SkillGain:Crafting", 1u);

                return UpdateSkillPro(spellIdx.SkillupSkillLineID,
                                      SkillGainChance(skillValue,
                                                      spellIdx.TrivialSkillLineRankHigh,
                                                      (uint)(spellIdx.TrivialSkillLineRankHigh + spellIdx.TrivialSkillLineRankLow) / 2,
                                                      spellIdx.TrivialSkillLineRankLow),
                                      craftSkillGain);
            }

        return false;
    }

    public void UpdateEquipSpellsAtFormChange()
    {
        for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            if (_items[i] != null && !_items[i].IsBroken && CanUseAttackType(PlayerComputators.GetAttackBySlot(i, _items[i].Template.InventoryType)))
            {
                ApplyItemEquipSpell(_items[i], false, true); // remove spells that not fit to form
                ApplyItemEquipSpell(_items[i], true, true);  // add spells that fit form but not active
            }

        UpdateItemSetAuras(true);
    }

    public bool UpdateFishingSkill()
    {
        Log.Logger.Debug("UpdateFishingSkill");

        uint skillValue = GetPureSkillValue(SkillType.ClassicFishing);

        if (skillValue >= GetMaxSkillValue(SkillType.ClassicFishing))
            return false;

        var stepsNeededToLevelUp = GetFishingStepsNeededToLevelUp(skillValue);
        ++_fishingSteps;

        if (_fishingSteps < stepsNeededToLevelUp)
            return false;

        _fishingSteps = 0;

        var gatheringSkillGain = Configuration.GetDefaultValue("SkillGain:Gathering", 1u);

        return UpdateSkillPro(SkillType.ClassicFishing, 100 * 10, gatheringSkillGain);

    }

    public bool UpdateGatherSkill(uint skillId, uint skillValue, uint redLevel, uint multiplicator = 1, WorldObject obj = null)
    {
        return UpdateGatherSkill((SkillType)skillId, skillValue, redLevel, multiplicator, obj);
    }

    public bool UpdateGatherSkill(SkillType skillId, uint skillValue, uint redLevel, uint multiplicator = 1, WorldObject obj = null)
    {
        Log.Logger.Debug("UpdateGatherSkill(SkillId {0} SkillLevel {1} RedLevel {2})", skillId, skillValue, redLevel);

        var gatheringSkillGain = Configuration.GetDefaultValue("SkillGain:Gathering", 1u);

        var grayLevel = redLevel + 100;
        var greenLevel = redLevel + 50;
        var yellowLevel = redLevel + 25;

        var go = obj?.AsGameObject;

        if (go != null)
        {
            if (go.Template.GetTrivialSkillLow() != 0)
                yellowLevel = go.Template.GetTrivialSkillLow();

            if (go.Template.GetTrivialSkillHigh() != 0)
                grayLevel = go.Template.GetTrivialSkillHigh();

            greenLevel = (yellowLevel + grayLevel) / 2;
        }

        // For skinning and Mining chance decrease with level. 1-74 - no decrease, 75-149 - 2 times, 225-299 - 8 times
        switch (skillId)
        {
            case SkillType.Herbalism:
            case SkillType.ClassicHerbalism:
            case SkillType.OutlandHerbalism:
            case SkillType.NorthrendHerbalism:
            case SkillType.CataclysmHerbalism:
            case SkillType.PandariaHerbalism:
            case SkillType.DraenorHerbalism:
            case SkillType.LegionHerbalism:
            case SkillType.KulTiranHerbalism:
            case SkillType.Jewelcrafting:
            case SkillType.Inscription:
            case SkillType.DragonIslesHerbalism:
            case SkillType.DragonIslesInscription:
            case SkillType.DragonIslesJewelcrafting:
                return UpdateSkillPro(skillId, SkillGainChance(skillValue, grayLevel, greenLevel, yellowLevel) * (int)multiplicator, gatheringSkillGain);

            case SkillType.Skinning:
            case SkillType.ClassicSkinning:
            case SkillType.OutlandSkinning:
            case SkillType.NorthrendSkinning:
            case SkillType.CataclysmSkinning:
            case SkillType.PandariaSkinning:
            case SkillType.DraenorSkinning:
            case SkillType.LegionSkinning:
            case SkillType.KulTiranSkinning:
            case SkillType.DragonIslesSkinning:
                if (Configuration.GetDefaultValue("SkillChance:SkinningSteps", 75) == 0)
                    return UpdateSkillPro(skillId, SkillGainChance(skillValue, grayLevel, greenLevel, yellowLevel) * (int)multiplicator, gatheringSkillGain);

                return UpdateSkillPro(skillId, (int)(SkillGainChance(skillValue, grayLevel, greenLevel, yellowLevel) * multiplicator) >> (int)(skillValue / Configuration.GetDefaultValue("SkillChance:SkinningSteps", 75)), gatheringSkillGain);

            case SkillType.Mining:
            case SkillType.ClassicMining:
            case SkillType.OutlandMining:
            case SkillType.NorthrendMining:
            case SkillType.CataclysmMining:
            case SkillType.PandariaMining:
            case SkillType.DraenorMining:
            case SkillType.LegionMining:
            case SkillType.KulTiranMining:
            case SkillType.DragonIslesMining:
                if (Configuration.GetDefaultValue("SkillChance:MiningSteps", 75) == 0)
                    return UpdateSkillPro(skillId, SkillGainChance(skillValue, grayLevel, greenLevel, yellowLevel) * (int)multiplicator, gatheringSkillGain);

                return UpdateSkillPro(skillId, (int)(SkillGainChance(skillValue, grayLevel, greenLevel, yellowLevel) * multiplicator) >> (int)(skillValue / Configuration.GetDefaultValue("SkillChance:MiningSteps", 75)), gatheringSkillGain);
        }

        return false;
    }

    public void UpdatePotionCooldown(Spell spell = null)
    {
        // no potion used i combat or still in combat
        if (_lastPotionId == 0 || IsInCombat)
            return;

        // Call not from spell cast, send cooldown event for item spells if no in combat
        if (spell == null)
        {
            // spell/item pair let set proper cooldown (except not existed charged spell cooldown spellmods for potions)
            var proto = GameObjectManager.GetItemTemplate(_lastPotionId);

            if (proto != null)
                for (byte idx = 0; idx < proto.Effects.Count; ++idx)
                    if (proto.Effects[idx].SpellID != 0 && proto.Effects[idx].TriggerType == ItemSpelltriggerType.OnUse)
                    {
                        var spellInfo = SpellManager.GetSpellInfo((uint)proto.Effects[idx].SpellID);

                        if (spellInfo != null)
                            SpellHistory.SendCooldownEvent(spellInfo, _lastPotionId);
                    }
        }
        // from spell cases (m_lastPotionId set in Spell.SendSpellCooldown)
        else
        {
            if (spell is { IsIgnoringCooldowns: true })
                return;

            SpellHistory.SendCooldownEvent(spell.SpellInfo, _lastPotionId, spell);
        }

        _lastPotionId = 0;
    }

    public bool UpdateSkillPro(SkillType skillId, int chance, uint step)
    {
        return UpdateSkillPro((uint)skillId, chance, step);
    }

    public bool UpdateSkillPro(uint skillId, int chance, uint step)
    {
        // levels sync. with spell requirement for skill levels to learn
        // bonus abilities in sSkillLineAbilityStore
        // Used only to avoid scan DBC at each skill grow
        uint[] bonusSkillLevels =
        {
            75, 150, 225, 300, 375, 450, 525, 600, 700, 850
        };

        Log.Logger.Debug("UpdateSkillPro(SkillId {0}, Chance {1:D3}%)", skillId, chance / 10.0f);

        if (skillId == 0)
            return false;

        if (chance <= 0) // speedup in 0 chance case
        {
            Log.Logger.Debug("Player:UpdateSkillPro Chance={0:D3}% missed", chance / 10.0f);

            return false;
        }

        var skillStatusData = _skillStatus.LookupByKey(skillId);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
            return false;

        SkillInfo skillInfoField = ActivePlayerData.Skill;

        var value = skillInfoField.SkillRank[skillStatusData.Pos];
        var max = skillInfoField.SkillMaxRank[skillStatusData.Pos];

        if (max == 0 || value == 0 || value >= max)
            return false;

        if (RandomHelper.IRand(1, 1000) > chance)
        {
            Log.Logger.Debug("Player:UpdateSkillPro Chance={0:F3}% missed", chance / 10.0f);

            return false;
        }

        var newValue = (ushort)(value + step);

        if (newValue > max)
            newValue = max;

        SetSkillRank(skillStatusData.Pos, newValue);

        if (skillStatusData.State != SkillState.New)
            skillStatusData.State = SkillState.Changed;

        if (bonusSkillLevels.Any(bsl => value < bsl && newValue >= bsl))
            LearnSkillRewardedSpells(skillId, newValue, Race);

        UpdateSkillEnchantments(skillId, value, newValue);
        UpdateCriteria(CriteriaType.SkillRaised, skillId);
        Log.Logger.Debug("Player:UpdateSkillPro Chance={0:F3}% taken", chance / 10.0f);

        return true;
    }

    public void UpdateSkillsForLevel()
    {
        var race = Race;
        var maxSkill = GetMaxSkillValueForLevel();
        SkillInfo skillInfoField = ActivePlayerData.Skill;

        foreach (var pair in _skillStatus)
        {
            if (pair.Value.State == SkillState.Deleted || skillInfoField.SkillRank[pair.Value.Pos] == 0)
                continue;

            var pskill = pair.Key;
            var rcEntry = DB2Manager.GetSkillRaceClassInfo(pskill, Race, Class);

            if (rcEntry == null)
                continue;

            if (SpellManager.GetSkillRangeType(rcEntry) == SkillRangeType.Level)
            {
                if (rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
                    SetSkillRank(pair.Value.Pos, maxSkill);

                SetSkillMaxRank(pair.Value.Pos, maxSkill);

                if (pair.Value.State != SkillState.New)
                    pair.Value.State = SkillState.Changed;
            }

            // Update level dependent skillline spells
            LearnSkillRewardedSpells(rcEntry.SkillID, skillInfoField.SkillRank[pair.Value.Pos], race);
        }
    }

    public void UpdateWeaponDependentAuras(WeaponAttackType attackType)
    {
        UpdateWeaponDependentCritAuras(attackType);
        UpdateDamageDoneMods(attackType);
        UpdateDamagePctDoneMods(attackType);
    }

    public void UpdateZoneDependentAuras(uint newZone)
    {
        // Some spells applied at enter into zone (with subzones), aura removed in UpdateAreaDependentAuras that called always at zone.area update
        var saBounds = SpellManager.GetSpellAreaForAreaMapBounds(newZone);

        foreach (var spell in saBounds)
            if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, newZone, 0))
                if (!HasAura(spell.SpellId))
                    SpellFactory.CastSpell(this, spell.SpellId, true);
    }

    private void AddEnchantmentDuration(Item item, EnchantmentSlot slot, uint duration)
    {
        if (item == null)
            return;

        if (slot >= EnchantmentSlot.Max)
            return;

        for (var i = 0; i < _enchantDurations.Count; ++i)
        {
            var enchantDuration = _enchantDurations[i];

            if (enchantDuration.Item == item && enchantDuration.Slot == slot)
            {
                enchantDuration.Item.SetEnchantmentDuration(enchantDuration.Slot, enchantDuration.Leftduration, this);
                _enchantDurations.Remove(enchantDuration);

                break;
            }
        }

        if (duration <= 0)
            return;

        Session.PacketRouter.OpCodeHandler<ItemHandler>().SendItemEnchantTimeUpdate(GUID, item.GUID, (uint)slot, duration / 1000);
        _enchantDurations.Add(new EnchantDuration(item, slot, duration));
    }

    private void AddEnchantmentDurations(Item item)
    {
        for (EnchantmentSlot x = 0; x < EnchantmentSlot.Max; ++x)
        {
            if (item.GetEnchantmentId(x) == 0)
                continue;

            var duration = item.GetEnchantmentDuration(x);

            if (duration > 0)
                AddEnchantmentDuration(item, x, duration);
        }
    }

    private bool AddSpell(uint spellId, bool active, bool learning, bool dependent, bool disabled, bool loading = false, uint fromSkill = 0, bool favorite = false, int? traitDefinitionId = null)
    {
        var spellInfo = SpellManager.GetSpellInfo(spellId);

        if (spellInfo == null)
        {
            // do character spell book cleanup (all characters)
            if (!Location.IsInWorld && !learning)
            {
                Log.Logger.Error("Player.AddSpell: Spell (ID: {0}) does not exist. deleting for all characters in `character_spell`.", spellId);

                DeleteSpellFromAllPlayers(spellId);
            }
            else
                Log.Logger.Error("Player.AddSpell: Spell (ID: {0}) does not exist", spellId);

            return false;
        }

        if (!SpellManager.IsSpellValid(spellInfo, this, false))
        {
            // do character spell book cleanup (all characters)
            if (!Location.IsInWorld && !learning)
            {
                Log.Logger.Error("Player.AddSpell: Spell (ID: {0}) is invalid. deleting for all characters in `character_spell`.", spellId);

                DeleteSpellFromAllPlayers(spellId);
            }
            else
                Log.Logger.Error("Player.AddSpell: Spell (ID: {0}) is invalid", spellId);

            return false;
        }

        var state = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

        var dependentSet = false;
        var disabledCase = false;
        var supercededOld = false;

        var playerSpell = _spells.LookupByKey(spellId);

        if (playerSpell is { State: PlayerSpellState.Temporary })
            RemoveTemporarySpell(spellId);

        if (playerSpell != null)
        {
            uint nextActiveSpellID = 0;

            // fix activate state for non-stackable low rank (and find next spell for !active case)
            if (spellInfo.IsRanked)
            {
                var next = SpellManager.GetNextSpellInChain(spellId);

                if (next != 0)
                    if (HasSpell(next))
                    {
                        // high rank already known so this must !active
                        active = false;
                        nextActiveSpellID = next;
                    }
            }

            // not do anything if already known in expected state
            if (playerSpell.State != PlayerSpellState.Removed &&
                playerSpell.Active == active &&
                playerSpell.Dependent == dependent &&
                playerSpell.Disabled == disabled)
            {
                if (!Location.IsInWorld && !learning)
                    playerSpell.State = PlayerSpellState.Unchanged;

                return false;
            }

            // dependent spell known as not dependent, overwrite state
            if (playerSpell.State != PlayerSpellState.Removed && !playerSpell.Dependent && dependent)
            {
                playerSpell.Dependent = true;

                if (playerSpell.State != PlayerSpellState.New)
                    playerSpell.State = PlayerSpellState.Changed;

                dependentSet = true;
            }

            if (playerSpell.TraitDefinitionId != traitDefinitionId)
            {
                if (playerSpell.TraitDefinitionId.HasValue)
                    if (CliDB.TraitDefinitionStorage.TryGetValue((uint)playerSpell.TraitDefinitionId.Value, out var traitDefinition))
                        RemoveOverrideSpell(traitDefinition.OverridesSpellID, spellId);

                playerSpell.TraitDefinitionId = traitDefinitionId;
            }

            playerSpell.Favorite = favorite;

            // update active state for known spell
            if (playerSpell.Active != active && playerSpell.State != PlayerSpellState.Removed && !playerSpell.Disabled)
            {
                playerSpell.Active = active;

                if (!Location.IsInWorld && !learning && !dependentSet) // explicitly load from DB and then exist in it already and set correctly
                    playerSpell.State = PlayerSpellState.Unchanged;
                else if (playerSpell.State != PlayerSpellState.New)
                    playerSpell.State = PlayerSpellState.Changed;

                if (active)
                {
                    if (spellInfo.IsPassive && HandlePassiveSpellLearn(spellInfo))
                        SpellFactory.CastSpell(this, spellId, true);
                }
                else if (Location.IsInWorld)
                {
                    if (nextActiveSpellID != 0)
                        SendSupercededSpell(spellId, nextActiveSpellID);
                    else
                    {
                        UnlearnedSpells removedSpells = new();
                        removedSpells.SpellID.Add(spellId);
                        SendPacket(removedSpells);
                    }
                }

                return active;
            }

            if (playerSpell.Disabled != disabled && playerSpell.State != PlayerSpellState.Removed)
            {
                if (playerSpell.State != PlayerSpellState.New)
                    playerSpell.State = PlayerSpellState.Changed;

                playerSpell.Disabled = disabled;

                if (disabled)
                    return false;

                disabledCase = true;
            }
            else
                switch (playerSpell.State)
                {
                    case PlayerSpellState.Unchanged:
                        return false;

                    case PlayerSpellState.Removed:
                    {
                        _spells.Remove(spellId);
                        state = PlayerSpellState.Changed;

                        break;
                    }
                    default:
                    {
                        // can be in case spell loading but learned at some previous spell loading
                        if (!Location.IsInWorld && !learning && !dependentSet)
                            playerSpell.State = PlayerSpellState.Unchanged;

                        return false;
                    }
                }
        }

        if (!disabledCase) // skip new spell adding if spell already known (disabled spells case)
        {
            // non talent spell: learn low ranks (recursive call)
            var prevSpell = SpellManager.GetPrevSpellInChain(spellId);

            if (prevSpell != 0)
            {
                if (!Location.IsInWorld || disabled) // at spells loading, no output, but allow save
                    AddSpell(prevSpell, active, true, true, disabled, false, fromSkill);
                else // at normal learning
                    LearnSpell(prevSpell, true, fromSkill);
            }

            PlayerSpell newspell = new()
            {
                State = state,
                Active = active,
                Dependent = dependent,
                Disabled = disabled,
                Favorite = favorite
            };

            if (traitDefinitionId.HasValue)
                newspell.TraitDefinitionId = traitDefinitionId.Value;

            // replace spells in action bars and spellbook to bigger rank if only one spell rank must be accessible
            if (newspell.Active && !newspell.Disabled && spellInfo.IsRanked)
                foreach (var spell in _spells)
                {
                    if (spell.Value.State == PlayerSpellState.Removed)
                        continue;

                    var iSpellInfo = SpellManager.GetSpellInfo(spell.Key);

                    if (iSpellInfo == null)
                        continue;

                    if (spellInfo.IsDifferentRankOf(iSpellInfo))
                        if (spell.Value.Active)
                        {
                            if (spellInfo.IsHighRankOf(iSpellInfo))
                            {
                                if (Location.IsInWorld) // not send spell (re-/over-)learn packets at loading
                                    SendSupercededSpell(spell.Key, spellId);

                                // mark old spell as disable (SMSG_SUPERCEDED_SPELL replace it in client by new)
                                spell.Value.Active = false;

                                if (spell.Value.State != PlayerSpellState.New)
                                    spell.Value.State = PlayerSpellState.Changed;

                                supercededOld = true; // new spell replace old in action bars and spell book.
                            }
                            else
                            {
                                if (Location.IsInWorld) // not send spell (re-/over-)learn packets at loading
                                    SendSupercededSpell(spellId, spell.Key);

                                // mark new spell as disable (not learned yet for client and will not learned)
                                newspell.Active = false;

                                if (newspell.State != PlayerSpellState.New)
                                    newspell.State = PlayerSpellState.Changed;
                            }
                        }
                }

            _spells[spellId] = newspell;

            // return false if spell disabled
            if (newspell.Disabled)
                return false;
        }

        var castSpell = false;

        // cast talents with SPELL_EFFECT_LEARN_SPELL (other dependent spells will learned later as not auto-learned)
        // note: all spells with SPELL_EFFECT_LEARN_SPELL isn't passive
        if (!loading && spellInfo.HasAttribute(SpellCustomAttributes.IsTalent) && spellInfo.HasEffect(SpellEffectName.LearnSpell))
            // ignore stance requirement for talent learn spell (stance set for spell only for client spell description show)
            castSpell = true;
        // also cast passive spells (including all talents without SPELL_EFFECT_LEARN_SPELL) with additional checks
        else if (spellInfo.IsPassive)
            castSpell = HandlePassiveSpellLearn(spellInfo);
        else if (spellInfo.HasEffect(SpellEffectName.SkillStep))
            castSpell = true;
        else if (spellInfo.HasAttribute(SpellAttr1.CastWhenLearned))
            castSpell = true;

        if (castSpell)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

            if (traitDefinitionId.HasValue)
            {
                var traitConfig = GetTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID);

                if (traitConfig != null)
                {
                    var traitEntryIndex = traitConfig.Entries.FindIndexIf(traitEntry => CliDB.TraitNodeEntryStorage.LookupByKey(traitEntry.TraitNodeEntryID)?.TraitDefinitionID == traitDefinitionId);

                    var rank = 0;

                    if (traitEntryIndex >= 0)
                        rank = traitConfig.Entries[traitEntryIndex].Rank + traitConfig.Entries[traitEntryIndex].GrantedRanks;

                    if (rank > 0)
                    {
                        var traitDefinitionEffectPoints = TraitMgr.GetTraitDefinitionEffectPointModifiers(traitDefinitionId.Value);

                        if (traitDefinitionEffectPoints != null)
                            foreach (var traitDefinitionEffectPoint in traitDefinitionEffectPoints)
                            {
                                if (traitDefinitionEffectPoint.EffectIndex >= spellInfo.Effects.Count)
                                    continue;

                                double basePoints = DB2Manager.GetCurveValueAt((uint)traitDefinitionEffectPoint.CurveID, rank);

                                if (traitDefinitionEffectPoint.GetOperationType() == TraitPointsOperationType.Multiply)
                                    basePoints *= spellInfo.GetEffect(traitDefinitionEffectPoint.EffectIndex).CalcBaseValue(this, null, 0, -1);

                                args.AddSpellMod(SpellValueMod.BasePoint0 + traitDefinitionEffectPoint.EffectIndex, basePoints);
                            }
                    }
                }
            }

            SpellFactory.CastSpell(this, spellId, args);

            if (spellInfo.HasEffect(SpellEffectName.SkillStep))
                return false;
        }

        if (traitDefinitionId.HasValue)
            if (CliDB.TraitDefinitionStorage.TryGetValue((uint)traitDefinitionId.Value, out var traitDefinition))
                AddOverrideSpell(traitDefinition.OverridesSpellID, spellId);

        // update free primary prof.points (if any, can be none in case GM .learn prof. learning)
        var freeProfs = FreePrimaryProfessionPoints;

        if (freeProfs != 0)
            if (spellInfo.IsPrimaryProfessionFirstRank)
                SetFreePrimaryProfessions(freeProfs - 1);

        var skillBounds = SpellManager.GetSkillLineAbilityMapBounds(spellId);

        var spellLearnSkill = SpellManager.GetSpellLearnSkill(spellId);

        if (spellLearnSkill != null)
        {
            // add dependent skills if this spell is not learned from adding skill already
            if ((uint)spellLearnSkill.Skill != fromSkill)
            {
                var skillValue = GetPureSkillValue(spellLearnSkill.Skill);
                var skillMaxValue = GetPureMaxSkillValue(spellLearnSkill.Skill);

                if (skillValue < spellLearnSkill.Value)
                    skillValue = spellLearnSkill.Value;

                var newSkillMaxValue = spellLearnSkill.Maxvalue == 0 ? GetMaxSkillValueForLevel() : spellLearnSkill.Maxvalue;

                if (skillMaxValue < newSkillMaxValue)
                    skillMaxValue = newSkillMaxValue;

                SetSkill(spellLearnSkill.Skill, spellLearnSkill.Step, skillValue, skillMaxValue);
            }
        }
        else
            // not ranked skills
            foreach (var spellIdx in skillBounds)
            {
                if (!CliDB.SkillLineStorage.ContainsKey(spellIdx.SkillLine))
                    continue;

                if (spellIdx.SkillLine == fromSkill)
                    continue;

                // Runeforging special case
                if ((spellIdx.AcquireMethod == AbilityLearnType.OnSkillLearn && !HasSkill((SkillType)spellIdx.SkillLine)) || (spellIdx.SkillLine == (int)SkillType.Runeforging && spellIdx.TrivialSkillLineRankHigh == 0))
                {
                    var rcInfo = DB2Manager.GetSkillRaceClassInfo(spellIdx.SkillLine, Race, Class);

                    if (rcInfo != null)
                        LearnDefaultSkill(rcInfo);
                }
            }

        // learn dependent spells
        var spellBounds = SpellManager.GetSpellLearnSpellMapBounds(spellId);

        foreach (var spellNode in spellBounds)
        {
            if (!spellNode.AutoLearned)
            {
                if (!Location.IsInWorld || !spellNode.Active) // at spells loading, no output, but allow save
                    AddSpell(spellNode.Spell, spellNode.Active, true, true, false);
                else // at normal learning
                    LearnSpell(spellNode.Spell, true);
            }

            if (spellNode.OverridesSpell != 0 && spellNode.Active)
                AddOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
        }

        if (!Session.PlayerLoading)
        {
            // not ranked skills
            foreach (var spellIdx in skillBounds)
            {
                UpdateCriteria(CriteriaType.LearnTradeskillSkillLine, spellIdx.SkillLine);
                UpdateCriteria(CriteriaType.LearnSpellFromSkillLine, spellIdx.SkillLine);
            }

            UpdateCriteria(CriteriaType.LearnOrKnowSpell, spellId);
        }

        // needs to be when spell is already learned, to prevent infinite recursion crashes
        if (DB2Manager.GetMount(spellId) != null)
            Session.CollectionMgr.AddMount(spellId, MountStatusFlags.None, false, !Location.IsInWorld);

        // return true (for send learn packet) only if spell active (in case ranked spells) and not replace old spell
        return active && !disabled && !supercededOld;
    }

    private void ApplyEnchantment(Item item, bool apply)
    {
        for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
            ApplyEnchantment(item, slot, apply);
    }

    private void ApplyItemObtainSpells(Item item, bool apply)
    {
        if (item.Template.HasFlag(ItemFlags.Legacy))
            return;

        foreach (var effect in item.Effects)
        {
            if (effect.TriggerType != ItemSpelltriggerType.OnPickup) // On obtain trigger
                continue;

            var spellId = effect.SpellID;

            if (spellId <= 0)
                continue;

            if (apply)
            {
                if (!HasAura((uint)spellId))
                    SpellFactory.CastSpell(this, (uint)spellId, new CastSpellExtraArgs().SetCastItem(item));
            }
            else
                RemoveAura((uint)spellId);
        }
    }

    private void CastAllObtainSpells()
    {
        var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

        for (var slot = InventorySlots.ItemStart; slot < inventoryEnd; ++slot)
        {
            var item = GetItemByPos(InventorySlots.Bag0, slot);

            if (item != null)
                ApplyItemObtainSpells(item, true);
        }

        for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
        {
            var bag = GetBagByPos(i);

            if (bag == null)
                continue;

            for (byte slot = 0; slot < bag.GetBagSize(); ++slot)
            {
                var item = bag.GetItemByPos(slot);

                if (item != null)
                    ApplyItemObtainSpells(item, true);
            }
        }
    }

    private void CorrectMetaGemEnchants(byte exceptslot, bool apply)
    {
        //cycle all equipped items
        for (var slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
        {
            //enchants for the slot being socketed are handled by Player.ApplyItemMods
            if (slot == exceptslot)
                continue;

            var pItem = GetItemByPos(InventorySlots.Bag0, slot);

            if (pItem == null || pItem.GetSocketColor(0) == 0)
                continue;

            for (var enchantSlot = EnchantmentSlot.Sock1; enchantSlot < EnchantmentSlot.Sock3; ++enchantSlot)
            {
                var enchantID = pItem.GetEnchantmentId(enchantSlot);

                if (enchantID == 0)
                    continue;

                if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var enchantEntry))
                    continue;

                uint condition = enchantEntry.ConditionID;

                if (condition != 0)
                {
                    //was enchant active with/without item?
                    var wasactive = EnchantmentFitsRequirements(condition, (sbyte)(apply ? exceptslot : -1));

                    //should it now be?
                    if (wasactive ^ EnchantmentFitsRequirements(condition, (sbyte)(apply ? -1 : exceptslot)))
                        // ignore item gem conditions
                        //if state changed, (dis)apply enchant
                        ApplyEnchantment(pItem, enchantSlot, !wasactive, true, true);
                }
            }
        }
    }

    private bool EnchantmentFitsRequirements(uint enchantmentcondition, sbyte slot)
    {
        if (enchantmentcondition == 0)
            return true;

        if (!CliDB.SpellItemEnchantmentConditionStorage.TryGetValue(enchantmentcondition, out var condition))
            return true;

        byte[] curcount =
        {
            0, 0, 0, 0
        };

        //counting current equipped gem colors
        for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
        {
            if (i == slot)
                continue;

            var pItem2 = GetItemByPos(InventorySlots.Bag0, i);

            if (pItem2 is { IsBroken: false })
                foreach (var gemData in pItem2.ItemData.Gems)
                {
                    var gemProto = GameObjectManager.GetItemTemplate(gemData.ItemId);

                    if (gemProto == null)
                        continue;

                    if (!CliDB.GemPropertiesStorage.TryGetValue(gemProto.GemProperties, out var gemProperty))
                        continue;

                    var gemColor = (uint)gemProperty.Type;

                    for (byte b = 0, tmpcolormask = 1; b < 4; b++, tmpcolormask <<= 1)
                        if (Convert.ToBoolean(tmpcolormask & gemColor))
                            ++curcount[b];
                }
        }

        var activate = true;

        for (byte i = 0; i < 5; i++)
        {
            if (condition.LtOperandType[i] == 0)
                continue;

            uint curGem = curcount[condition.LtOperandType[i] - 1];

            // if have <CompareColor> use them as count, else use <value> from Condition
            uint cmpGem = condition.RtOperandType[i] != 0 ? curcount[condition.RtOperandType[i] - 1] : condition.RtOperand[i];

            switch (condition.Operator[i])
            {
                case 2: // requires less <color> than (<value> || <comparecolor>) gems
                    activate &= curGem < cmpGem;

                    break;

                case 3: // requires more <color> than (<value> || <comparecolor>) gems
                    activate &= curGem > cmpGem;

                    break;

                case 5: // requires at least <color> than (<value> || <comparecolor>) gems
                    activate &= curGem >= cmpGem;

                    break;
            }
        }

        Log.Logger.Debug("Checking Condition {0}, there are {1} Meta Gems, {2} Red Gems, {3} Yellow Gems and {4} Blue Gems, Activate:{5}", enchantmentcondition, curcount[0], curcount[1], curcount[2], curcount[3], activate ? "yes" : "no");

        return activate;
    }

    private int FindEmptyProfessionSlotFor(uint skillId)
    {
        if (!CliDB.SkillLineStorage.TryGetValue(skillId, out var skillEntry))
            return -1;

        if (skillEntry.ParentSkillLineID != 0 || skillEntry.CategoryID != SkillCategory.Profession)
            return -1;

        var index = 0;

        // if there is no same profession, find any free slot
        foreach (var b in ActivePlayerData.ProfessionSkillLine)
        {
            if (b == 0)
                return index;

            index++;
        }

        return -1;
    }

    private byte GetFishingStepsNeededToLevelUp(uint skillValue)
    {
        return skillValue switch
        {
            // These formulas are guessed to be as close as possible to how the skill difficulty curve for fishing was on Retail.
            < 75   => 1,
            <= 300 => (byte)(skillValue / 44),
            _      => (byte)(skillValue / 31)
        };
    }

    private ushort GetMaxSkillValue(SkillType skill)
    {
        if (skill == 0)
            return 0;

        SkillInfo skillInfo = ActivePlayerData.Skill;

        var skillStatusData = _skillStatus.LookupByKey(skill);

        if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
            return 0;

        int result = skillInfo.SkillMaxRank[skillStatusData.Pos];
        result += skillInfo.SkillTempBonus[skillStatusData.Pos];
        result += skillInfo.SkillPermBonus[skillStatusData.Pos];

        return (ushort)(result < 0 ? 0 : result);
    }

    private float GetWeaponProcChance()
    {
        // normalized proc chance for weapon attack speed
        // (odd formula...)
        if (IsAttackReady())
            return GetBaseAttackTime(WeaponAttackType.BaseAttack) * 1.8f / 1000.0f;

        if (HasOffhandWeapon && IsAttackReady(WeaponAttackType.OffAttack))
            return GetBaseAttackTime(WeaponAttackType.OffAttack) * 1.6f / 1000.0f;

        return 0;
    }

    private bool HandlePassiveSpellLearn(SpellInfo spellInfo)
    {
        // note: form passives activated with shapeshift spells be implemented by HandleShapeshiftBoosts instead of spell_learn_spell
        // talent dependent passives activated at form apply have proper stance data
        var form = ShapeshiftForm;

        var needCast = spellInfo.Stances == 0 ||
                       (form != 0 && Convert.ToBoolean(spellInfo.Stances & (1ul << ((int)form - 1)))) ||
                       (form == 0 && spellInfo.HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm));

        // Check EquippedItemClass
        // passive spells which apply aura and have an item requirement are to be added manually, instead of casted
        if (spellInfo.EquippedItemClass >= 0)
            foreach (var spellEffectInfo in spellInfo.Effects)
                if (spellEffectInfo.IsAura)
                {
                    if (!HasAura(spellInfo.Id) && HasItemFitToSpellRequirements(spellInfo))
                        AddAura(spellInfo.Id, this);

                    return false;
                }

        //Check CasterAuraStates
        return needCast && (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState));
    }

    private void InitializeSelfResurrectionSpells()
    {
        ClearSelfResSpell();

        var spells = new uint[3];

        var dummyAuras = GetAuraEffectsByType(AuraType.Dummy);

        foreach (var auraEffect in dummyAuras)
            // Soulstone Resurrection                           // prio: 3 (max, non death persistent)
            if (auraEffect.SpellInfo.SpellFamilyName == SpellFamilyNames.Warlock && auraEffect.SpellInfo.SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
                spells[0] = 3026;
            // Twisting Nether                                  // prio: 2 (max)
            else if (auraEffect.Id == 23701 && RandomHelper.randChance(10))
                spells[1] = 23700;

        // Reincarnation (passive spell)  // prio: 1
        if (HasSpell(20608) && !SpellHistory.HasCooldown(21169))
            spells[2] = 21169;

        foreach (var selfResSpell in spells)
            if (selfResSpell != 0)
                AddSelfResSpell(selfResSpell);
    }

    private void InitializeSkillFields()
    {
        uint i = 0;

        foreach (var skillLine in CliDB.SkillLineStorage.Values)
        {
            var rcEntry = DB2Manager.GetSkillRaceClassInfo(skillLine.Id, Race, Class);

            if (rcEntry == null)
                continue;

            SetSkillLineId(i, (ushort)skillLine.Id);
            SetSkillStartingRank(i, 1);
            _skillStatus.Add(skillLine.Id, new SkillStatusData(i, SkillState.Unchanged));

            if (++i >= SkillConst.MaxPlayerSkills)
                break;
        }
    }

    private bool IsAffectedBySpellmod(SpellInfo spellInfo, SpellModifier mod, Spell spell)
    {
        if (mod == null || spellInfo == null)
            return false;

        // First time this aura applies a mod to us and is out of charges
        if (spell != null && mod.OwnerAura.IsUsingCharges && mod.OwnerAura.Charges == 0 && !spell.AppliedMods.Contains(mod.OwnerAura))
            return false;

        switch (mod.Op)
        {
            case SpellModOp.Duration: // +duration to infinite duration spells making them limited
                if (spellInfo.Duration == -1)
                    return false;

                break;

            case SpellModOp.CritChance: // mod crit to spells that can't crit
                if (!spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
                    return false;

                break;

            case SpellModOp.PointsIndex0: // check if spell has any effect at that index
            case SpellModOp.Points:
                if (spellInfo.Effects.Count <= 0)
                    return false;

                break;

            case SpellModOp.PointsIndex1: // check if spell has any effect at that index
                if (spellInfo.Effects.Count <= 1)
                    return false;

                break;

            case SpellModOp.PointsIndex2: // check if spell has any effect at that index
                if (spellInfo.Effects.Count <= 2)
                    return false;

                break;

            case SpellModOp.PointsIndex3: // check if spell has any effect at that index
                if (spellInfo.Effects.Count <= 3)
                    return false;

                break;

            case SpellModOp.PointsIndex4: // check if spell has any effect at that index
                if (spellInfo.Effects.Count <= 4)
                    return false;

                break;
        }

        return spellInfo.IsAffectedBySpellMod(mod);
    }

    private void RemoveEnchantmentDurations(Item item)
    {
        for (var i = 0; i < _enchantDurations.Count; ++i)
        {
            var enchantDuration = _enchantDurations[i];

            if (enchantDuration.Item != item)
                continue;

            // save duration in item
            item.SetEnchantmentDuration(enchantDuration.Slot, enchantDuration.Leftduration, this);
            _enchantDurations.Remove(enchantDuration);
        }
    }

    private void RemoveEnchantmentDurationsReferences(Item item)
    {
        for (var i = 0; i < _enchantDurations.Count; ++i)
        {
            var enchantDuration = _enchantDurations[i];

            if (enchantDuration.Item == item)
                _enchantDurations.Remove(enchantDuration);
        }
    }

    private void RemoveItemDependentAurasAndCasts(Item pItem)
    {
        OwnedAurasList
            .CallOnMatch(aura =>
                         {
                             // skip not self applied auras
                             var spellInfo = aura.SpellInfo;

                             if (aura.CasterGuid != GUID)
                                 return false;

                             // skip if not item dependent or have alternative item
                             if (HasItemFitToSpellRequirements(spellInfo, pItem))
                                 return false;

                             // no alt item, remove aura, restart check
                             return true;
                         },
                         pair => RemoveOwnedAura(pair));

        // currently casted spells can be dependent from item
        for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
        {
            var spell = GetCurrentSpell(i);

            if (spell == null)
                continue;

            if (spell.State != SpellState.Delayed && !HasItemFitToSpellRequirements(spell.SpellInfo, pItem))
                InterruptSpell(i);
        }
    }

    private void RemoveSpecializationSpells()
    {
        for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
        {
            var specialization = DB2Manager.GetChrSpecializationByIndex(Class, i);

            if (specialization == null)
                continue;

            var specSpells = DB2Manager.GetSpecializationSpells(specialization.Id);

            if (specSpells != null)
                for (var j = 0; j < specSpells.Count; ++j)
                {
                    var specSpell = specSpells[j];
                    RemoveSpell(specSpell.SpellID, true);

                    if (specSpell.OverridesSpellID != 0)
                        RemoveOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
                }

            for (uint j = 0; j < PlayerConst.MaxMasterySpells; ++j)
            {
                var mastery = specialization.MasterySpellID[j];

                if (mastery != 0)
                    RemoveAura(mastery);
            }
        }
    }

    private void SendKnownSpells()
    {
        SendKnownSpells knownSpells = new()
        {
            InitialLogin = IsLoading
        };

        foreach (var spell in _spells.ToList())
        {
            if (spell.Value.State == PlayerSpellState.Removed)
                continue;

            if (!spell.Value.Active || spell.Value.Disabled)
                continue;

            knownSpells.KnownSpells.Add(spell.Key);

            if (spell.Value.Favorite)
                knownSpells.FavoriteSpells.Add(spell.Key);
        }

        SendPacket(knownSpells);
    }

    private void SendSpellModifiers()
    {
        SetSpellModifier flatMods = new(ServerOpcodes.SetFlatSpellModifier);
        SetSpellModifier pctMods = new(ServerOpcodes.SetPctSpellModifier);

        for (var i = 0; i < (int)SpellModOp.Max; ++i)
        {
            SpellModifierInfo flatMod = new();
            SpellModifierInfo pctMod = new();
            flatMod.ModIndex = pctMod.ModIndex = (byte)i;

            for (byte j = 0; j < 128; ++j)
            {
                FlagArray128 mask = new()
                {
                    [j / 32] = 1u << (j % 32)
                };

                SpellModifierData flatData;
                SpellModifierData pctData;

                flatData.ClassIndex = j;
                flatData.ModifierValue = 0.0f;
                pctData.ClassIndex = j;
                pctData.ModifierValue = 1.0f;

                foreach (var mod in _spellModifiers[i][(int)SpellModType.Flat].Cast<SpellModifierByClassMask>().Where(mod => mod.Mask & mask))
                    flatData.ModifierValue += mod.Value;


                foreach (var mod in _spellModifiers[i][(int)SpellModType.Pct].Cast<SpellModifierByClassMask>().Where(mod => mod.Mask & mask))
                    pctData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.Value);


                flatMod.ModifierData.Add(flatData);
                pctMod.ModifierData.Add(pctData);
            }

            flatMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 0.0f));

            pctMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 1.0f));

            flatMods.Modifiers.Add(flatMod);
            pctMods.Modifiers.Add(pctMod);
        }

        if (!flatMods.Modifiers.Empty())
            SendPacket(flatMods);

        if (!pctMods.Modifiers.Empty())
            SendPacket(pctMods);
    }

    private void SendSupercededSpell(uint oldSpell, uint newSpell)
    {
        SupercededSpells supercededSpells = new();

        LearnedSpellInfo learnedSpellInfo = new()
        {
            SpellID = newSpell,
            Superceded = (int)oldSpell
        };

        supercededSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
        SendPacket(supercededSpells);
    }

    private void SendUnlearnSpells()
    {
        SendPacket(new SendUnlearnSpells());
    }

    private int SkillGainChance(uint skillValue, uint grayLevel, uint greenLevel, uint yellowLevel)
    {
        if (skillValue >= grayLevel)
            return Configuration.GetDefaultValue("SkillChance:Grey", 0) * 10;

        if (skillValue >= greenLevel)
            return Configuration.GetDefaultValue("SkillChance:Green", 25) * 10;

        if (skillValue >= yellowLevel)
            return Configuration.GetDefaultValue("SkillChance:Yellow", 75) * 10;

        return Configuration.GetDefaultValue("SkillChance:Orange", 100) * 10;
    }

    private void UpdateEnchantTime(uint time)
    {
        for (var i = 0; i < _enchantDurations.Count; ++i)
        {
            var enchantDuration = _enchantDurations[i];

            if (enchantDuration.Item.GetEnchantmentId(enchantDuration.Slot) == 0)
                _enchantDurations.Remove(enchantDuration);
            else if (enchantDuration.Leftduration <= time)
            {
                ApplyEnchantment(enchantDuration.Item, enchantDuration.Slot, false, false);
                enchantDuration.Item.ClearEnchantment(enchantDuration.Slot);
                _enchantDurations.Remove(enchantDuration);
            }
            else if (enchantDuration.Leftduration > time)
                enchantDuration.Leftduration -= time;
        }
    }

    private void UpdateItemSetAuras(bool formChange = false)
    {
        // item set bonuses not dependent from item broken state
        foreach (var eff in ItemSetEff)
        {
            if (eff == null)
                continue;

            foreach (var itemSetSpell in eff.SetBonuses)
            {
                var spellInfo = SpellManager.GetSpellInfo(itemSetSpell.SpellID);

                if (itemSetSpell.ChrSpecID != 0 && itemSetSpell.ChrSpecID != GetPrimarySpecialization())
                    ApplyEquipSpell(spellInfo, null, false); // item set aura is not for current spec
                else
                {
                    ApplyEquipSpell(spellInfo, null, false, formChange); // remove spells that not fit to form - removal is skipped if shapeshift condition is satisfied
                    ApplyEquipSpell(spellInfo, null, true, formChange);  // add spells that fit form but not active
                }
            }
        }
    }

    private void UpdateSkillEnchantments(uint skillID, ushort currValue, ushort newValue)
    {
        for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            if (_items[i] != null)
                for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                {
                    var enchID = _items[i].GetEnchantmentId(slot);

                    if (enchID == 0)
                        continue;

                    if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchID, out var enchant))
                        return;

                    if (enchant.RequiredSkillID == skillID)
                    {
                        // Checks if the enchantment needs to be applied or removed
                        if (currValue < enchant.RequiredSkillRank && newValue >= enchant.RequiredSkillRank)
                            ApplyEnchantment(_items[i], slot, true);
                        else if (newValue < enchant.RequiredSkillRank && currValue >= enchant.RequiredSkillRank)
                            ApplyEnchantment(_items[i], slot, false);
                    }

                    // If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
                    // rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
                    if (slot is EnchantmentSlot.Sock1 or EnchantmentSlot.Sock2 or EnchantmentSlot.Sock3 && _items[i].GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
                    {
                        var pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(_items[i].GetEnchantmentId(EnchantmentSlot.Prismatic));

                        if (pPrismaticEnchant != null && pPrismaticEnchant.RequiredSkillID == skillID)
                        {
                            if (currValue < pPrismaticEnchant.RequiredSkillRank && newValue >= pPrismaticEnchant.RequiredSkillRank)
                                ApplyEnchantment(_items[i], slot, true);
                            else if (newValue < pPrismaticEnchant.RequiredSkillRank && currValue >= pPrismaticEnchant.RequiredSkillRank)
                                ApplyEnchantment(_items[i], slot, false);
                        }
                    }
                }
    }

    // this one rechecks weapon auras and stores them in BaseModGroup container
    // needed for things like axe specialization applying only to axe weapons in case of dual-wield
    private void UpdateWeaponDependentCritAuras(WeaponAttackType attackType)
    {
        BaseModGroup modGroup;

        switch (attackType)
        {
            case WeaponAttackType.BaseAttack:
                modGroup = BaseModGroup.CritPercentage;

                break;

            case WeaponAttackType.OffAttack:
                modGroup = BaseModGroup.OffhandCritPercentage;

                break;

            case WeaponAttackType.RangedAttack:
                modGroup = BaseModGroup.RangedCritPercentage;

                break;

            default:
                return;
        }

        double amount = 0.0f;
        amount += GetTotalAuraModifier(AuraType.ModWeaponCritPercent, auraEffect => CheckAttackFitToAuraRequirement(attackType, auraEffect));

        // these auras don't have item requirement (only Combat Expertise in 3.3.5a)
        amount += GetTotalAuraModifier(AuraType.ModCritPct);

        SetBaseModFlatValue(modGroup, amount);
    }
}