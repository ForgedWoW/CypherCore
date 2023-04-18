// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking.Packets.Duel;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public void _ApplyWeaponDamage(byte slot, Item item, bool apply)
    {
        var proto = item.Template;
        var attType = PlayerComputators.GetAttackBySlot(slot, proto.InventoryType);

        if (!IsInFeralForm && apply && !CanUseAttackType(attType))
            return;

        var damage = 0.0f;
        var itemLevel = item.GetItemLevel(this);
        proto.GetDamage(itemLevel, out var minDamage, out var maxDamage);

        if (minDamage > 0)
        {
            damage = apply ? minDamage : SharedConst.BaseMinDamage;
            SetBaseWeaponDamage(attType, WeaponDamageRange.MinDamage, damage);
        }

        if (maxDamage > 0)
        {
            damage = apply ? maxDamage : SharedConst.BaseMaxDamage;
            SetBaseWeaponDamage(attType, WeaponDamageRange.MaxDamage, damage);
        }

        if (CliDB.SpellShapeshiftFormStorage.ContainsKey((uint)ShapeshiftForm))
            SetBaseAttackTime(attType, apply ? proto.Delay : SharedConst.BaseAttackTime);

        var weaponBasedAttackPower = apply ? (int)(proto.GetDPS(itemLevel) * 6.0f) : 0;

        switch (attType)
        {
            case WeaponAttackType.BaseAttack:
                SetMainHandWeaponAttackPower(weaponBasedAttackPower);

                break;

            case WeaponAttackType.OffAttack:
                SetOffHandWeaponAttackPower(weaponBasedAttackPower);

                break;

            case WeaponAttackType.RangedAttack:
                SetRangedWeaponAttackPower(weaponBasedAttackPower);

                break;
        }

        if (CanModifyStats() && (damage != 0 || proto.Delay != 0))
            UpdateDamagePhysical(attType);
    }

    public void AddArmorProficiency(uint newflag)
    {
        _armorProficiency |= newflag;
    }

    public void AddWeaponProficiency(uint newflag)
    {
        _weaponProficiency |= newflag;
    }

    public override void AtEnterCombat()
    {
        base.AtEnterCombat();

        if (CombatManager.HasPvPCombat())
            EnablePvpRules(true);
    }

    public override void AtExitCombat()
    {
        base.AtExitCombat();
        UpdatePotionCooldown();
        _combatExitTime = Time.MSTime;
    }

    public void DuelComplete(DuelCompleteType type)
    {
        // duel not requested
        if (Duel == null)
            return;

        // Check if DuelComplete() has been called already up in the stack and in that case don't do anything else here
        if (Duel.State == DuelState.Completed)
            return;

        var opponent = Duel.Opponent;
        Duel.State = DuelState.Completed;
        opponent.Duel.State = DuelState.Completed;

        Log.Logger.Debug($"Duel Complete {GetName()} {opponent.GetName()}");

        DuelComplete duelCompleted = new()
        {
            Started = type != DuelCompleteType.Interrupted
        };

        SendPacket(duelCompleted);

        if (opponent.Session != null)
            opponent.SendPacket(duelCompleted);

        if (type != DuelCompleteType.Interrupted)
        {
            DuelWinner duelWinner = new()
            {
                BeatenName = type == DuelCompleteType.Won ? opponent.GetName() : GetName(),
                WinnerName = type == DuelCompleteType.Won ? GetName() : opponent.GetName(),
                BeatenVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
                WinnerVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
                Fled = type != DuelCompleteType.Won
            };

            SendMessageToSet(duelWinner, true);
        }

        opponent.DisablePvpRules();
        DisablePvpRules();

        ScriptManager.ForEach<IPlayerOnDuelEnd>(p => p.OnDuelEnd(type == DuelCompleteType.Won ? this : opponent,
                                                                 type == DuelCompleteType.Won ? opponent : this,
                                                                 type));

        switch (type)
        {
            case DuelCompleteType.Fled:
                // if initiator and opponent are on the same team
                // or initiator and opponent are not PvP enabled, forcibly stop attacking
                if (Team == opponent.Team)
                {
                    AttackStop();
                    opponent.AttackStop();
                }
                else
                {
                    if (!IsPvP)
                        AttackStop();

                    if (!opponent.IsPvP)
                        opponent.AttackStop();
                }

                break;

            case DuelCompleteType.Won:
                UpdateCriteria(CriteriaType.LoseDuel, 1);
                opponent.UpdateCriteria(CriteriaType.WinDuel, 1);

                // Credit for quest Death's Challenge
                if (Class == PlayerClass.Deathknight && opponent.GetQuestStatus(12733) == QuestStatus.Incomplete)
                    opponent.SpellFactory.CastSpell(Duel.Opponent, 52994, true);

                // Honor points after duel (the winner) - ImpConfig
                var amount = Configuration.GetDefaultValue("HonorPointsAfterDuel", 0);

                if (amount != 0)
                    opponent.RewardHonor(null, 1, amount);

                break;
        }

        // Victory emote spell
        if (type != DuelCompleteType.Interrupted)
            opponent.SpellFactory.CastSpell(Duel.Opponent, 52852, true);

        //Remove Duel Flag object
        var obj = Location.Map.GetGameObject(PlayerData.DuelArbiter);

        if (obj != null)
            Duel.Initiator.RemoveGameObject(obj, true);

        //remove auras
        opponent.GetAppliedAurasQuery().HasCasterGuid(GUID).IsPositive(false).AlsoMatches(appAur => appAur.Base.ApplyTime >= Duel.StartTime).Execute(RemoveAura);
        GetAppliedAurasQuery().HasCasterGuid(opponent.GUID).IsPositive(false).AlsoMatches(appAur => appAur.Base.ApplyTime >= Duel.StartTime).Execute(RemoveAura);

        // cleanup combo points
        ClearComboPoints();
        opponent.ClearComboPoints();

        //cleanups
        SetDuelArbiter(ObjectGuid.Empty);
        SetDuelTeam(0);
        opponent.SetDuelArbiter(ObjectGuid.Empty);
        opponent.SetDuelTeam(0);

        opponent.Duel = null;
        Duel = null;
    }

    public uint GetArmorProficiency()
    {
        return _armorProficiency;
    }

    public override float GetBlockPercent(uint attackerLevel)
    {
        var blockArmor = (float)ActivePlayerData.ShieldBlock;
        var armorConstant = DB2Manager.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, PlayerClass.None);

        if (blockArmor + armorConstant == 0)
            return 0;

        return Math.Min(blockArmor / (blockArmor + armorConstant), 0.85f);
    }

    public float GetExpertiseDodgeOrParryReduction(WeaponAttackType attType)
    {
        var baseExpertise = 7.5f;

        return attType switch
        {
            WeaponAttackType.BaseAttack => baseExpertise + ActivePlayerData.MainhandExpertise / 4.0f,
            WeaponAttackType.OffAttack  => baseExpertise + ActivePlayerData.OffhandExpertise / 4.0f,
            _                           => 0.0f
        };
    }

    public double GetRatingBonusValue(CombatRating cr)
    {
        var baseResult = ApplyRatingDiminishing(cr, ActivePlayerData.CombatRatings[(int)cr] * GetRatingMultiplier(cr));

        if (cr != CombatRating.ResiliencePlayerDamage)
            return baseResult;

        return (1.0f - Math.Pow(0.99f, baseResult)) * 100.0f;
    }

    public uint GetWeaponProficiency()
    {
        return _weaponProficiency;
    }

    public bool IsUseEquipedWeapon(bool mainhand)
    {
        // disarm applied only to mainhand weapon
        return !IsInFeralForm && (!mainhand || !HasUnitFlag(UnitFlags.Disarmed));
    }

    public void ResetContestedPvP()
    {
        ClearUnitState(UnitState.AttackPlayer);
        RemovePlayerFlag(PlayerFlags.ContestedPVP);
        _contestedPvPTimer = 0;
    }

    public void RestoreHealthAfterDuel()
    {
        SetHealth(_healthBeforeDuel);
    }

    public void RestoreManaAfterDuel()
    {
        SetPower(PowerType.Mana, (int)_manaBeforeDuel);
    }

    public void RewardPlayerAndGroupAtEvent(uint creatureID, WorldObject pRewardSource)
    {
        if (pRewardSource == null)
            return;

        var creatureGUID = pRewardSource.IsTypeId(TypeId.Unit) ? pRewardSource.GUID : ObjectGuid.Empty;

        // prepare data for near group iteration
        var group = Group;

        if (group != null)
            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var player = refe.Source;

                if (player == null)
                    continue;

                if (!player.IsAtGroupRewardDistance(pRewardSource))
                    continue; // member (alive or dead) or his corpse at req. distance

                // quest objectives updated only for alive group member or dead but with not released body
                if (player.IsAlive || player.Corpse == null)
                    player.KilledMonsterCredit(creatureID, creatureGUID);
            }
        else
            KilledMonsterCredit(creatureID, creatureGUID);
    }

    // duel health and mana reset methods
    public void SaveHealthBeforeDuel()
    {
        _healthBeforeDuel = (uint)Health;
    }

    public void SaveManaBeforeDuel()
    {
        _manaBeforeDuel = (uint)GetPower(PowerType.Mana);
    }

    public void SendProficiency(ItemClass itemClass, uint itemSubclassMask)
    {
        SetProficiency packet = new()
        {
            ProficiencyMask = itemSubclassMask,
            ProficiencyClass = (byte)itemClass
        };

        SendPacket(packet);
    }

    public void SetCanBlock(bool value)
    {
        if (CanBlock == value)
            return;

        CanBlock = value;
        UpdateBlockPercentage();
    }

    public void SetCanParry(bool value)
    {
        if (CanParry == value)
            return;

        CanParry = value;
        UpdateParryPercentage();
    }

    public void SetCanTitanGrip(bool value, uint penaltySpellId = 0)
    {
        if (value == _canTitanGrip)
            return;

        _canTitanGrip = value;
        _titanGripPenaltySpellId = penaltySpellId;
    }

    public void SetContestedPvP(Player attackedPlayer = null)
    {
        if (attackedPlayer != null && (attackedPlayer == this || (Duel != null && Duel.Opponent == attackedPlayer)))
            return;

        SetContestedPvPTimer(30000);

        if (!HasUnitState(UnitState.AttackPlayer))
        {
            AddUnitState(UnitState.AttackPlayer);
            SetPlayerFlag(PlayerFlags.ContestedPVP);
            // call MoveInLineOfSight for nearby contested guards
            AIRelocationNotifier notifier = new(this, GridType.World);
            CellCalculator.VisitGrid(this, notifier, Visibility.VisibilityRange);
        }

        foreach (var unit in Controlled)
            if (!unit.HasUnitState(UnitState.AttackPlayer))
            {
                unit.AddUnitState(UnitState.AttackPlayer);
                AIRelocationNotifier notifier = new(unit, GridType.World);
                CellCalculator.VisitGrid(this, notifier, Visibility.VisibilityRange);
            }
    }

    public void SetContestedPvPTimer(uint newTime)
    {
        _contestedPvPTimer = newTime;
    }

    public void SetDuelArbiter(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.DuelArbiter), guid);
    }

    public override void SetPvP(bool state)
    {
        base.SetPvP(state);

        foreach (var unit in Controlled)
            unit.SetPvP(state);
    }

    //PVP
    public void SetPvPDeath(bool on)
    {
        if (on)
            _extraFlags |= PlayerExtraFlags.PVPDeath;
        else
            _extraFlags &= ~PlayerExtraFlags.PVPDeath;
    }

    public void UpdateContestedPvP(uint diff)
    {
        if (_contestedPvPTimer == 0 || IsInCombat)
            return;

        if (_contestedPvPTimer <= diff)
            ResetContestedPvP();
        else
            _contestedPvPTimer -= diff;
    }

    public void UpdatePvP(bool state, bool overrideEndTime = false)
    {
        SetPvP(state);

        if (!state || overrideEndTime)
            PvpInfo.EndTimer = 0;
        else
            PvpInfo.EndTimer = GameTime.CurrentTime;
    }

    public void UpdatePvPFlag(long currTime)
    {
        if (!IsPvP)
            return;

        if (PvpInfo.EndTimer == 0 || currTime < PvpInfo.EndTimer + 300 || PvpInfo.IsHostile)
            return;

        if (PvpInfo.EndTimer <= currTime)
        {
            PvpInfo.EndTimer = 0;
            RemovePlayerFlag(PlayerFlags.PVPTimer);
        }

        UpdatePvP(false);
    }

    public void UpdatePvPState(bool onlyFfa = false)
    {
        // @todo should we always synchronize UNIT_FIELD_BYTES_2, 1 of controller and controlled?
        // no, we shouldn't, those are checked for affecting player by client
        if (!PvpInfo.IsInNoPvPArea && !IsGameMaster && (PvpInfo.IsInFfaPvPArea || WorldMgr.IsFFAPvPRealm || HasAuraType(AuraType.SetFFAPvp)))
        {
            if (!IsFFAPvP)
            {
                SetPvpFlag(UnitPVPStateFlags.FFAPvp);

                foreach (var unit in Controlled)
                    unit.SetPvpFlag(UnitPVPStateFlags.FFAPvp);
            }
        }
        else if (IsFFAPvP)
        {
            RemovePvpFlag(UnitPVPStateFlags.FFAPvp);

            foreach (var unit in Controlled)
                unit.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
        }

        if (onlyFfa)
            return;

        if (PvpInfo.IsHostile) // in hostile area
        {
            if (!IsPvP || PvpInfo.EndTimer != 0)
                UpdatePvP(true, true);
        }
        else // in friendly area
        {
            if (IsPvP && !HasPlayerFlag(PlayerFlags.InPVP) && PvpInfo.EndTimer == 0)
                PvpInfo.EndTimer = GameTime.CurrentTime; // start toggle-off
        }
    }

    private double ApplyRatingDiminishing(CombatRating cr, double bonusValue)
    {
        uint diminishingCurveId = cr switch
        {
            CombatRating.Dodge                  => DB2Manager.GetGlobalCurveId(GlobalCurve.DodgeDiminishing),
            CombatRating.Parry                  => DB2Manager.GetGlobalCurveId(GlobalCurve.ParryDiminishing),
            CombatRating.Block                  => DB2Manager.GetGlobalCurveId(GlobalCurve.BlockDiminishing),
            CombatRating.CritMelee              => DB2Manager.GetGlobalCurveId(GlobalCurve.CritDiminishing),
            CombatRating.CritRanged             => DB2Manager.GetGlobalCurveId(GlobalCurve.CritDiminishing),
            CombatRating.CritSpell              => DB2Manager.GetGlobalCurveId(GlobalCurve.CritDiminishing),
            CombatRating.Speed                  => DB2Manager.GetGlobalCurveId(GlobalCurve.SpeedDiminishing),
            CombatRating.Lifesteal              => DB2Manager.GetGlobalCurveId(GlobalCurve.LifestealDiminishing),
            CombatRating.HasteMelee             => DB2Manager.GetGlobalCurveId(GlobalCurve.HasteDiminishing),
            CombatRating.HasteRanged            => DB2Manager.GetGlobalCurveId(GlobalCurve.HasteDiminishing),
            CombatRating.HasteSpell             => DB2Manager.GetGlobalCurveId(GlobalCurve.HasteDiminishing),
            CombatRating.Avoidance              => DB2Manager.GetGlobalCurveId(GlobalCurve.AvoidanceDiminishing),
            CombatRating.Mastery                => DB2Manager.GetGlobalCurveId(GlobalCurve.MasteryDiminishing),
            CombatRating.VersatilityDamageDone  => DB2Manager.GetGlobalCurveId(GlobalCurve.VersatilityDoneDiminishing),
            CombatRating.VersatilityHealingDone => DB2Manager.GetGlobalCurveId(GlobalCurve.VersatilityDoneDiminishing),
            CombatRating.VersatilityDamageTaken => DB2Manager.GetGlobalCurveId(GlobalCurve.VersatilityTakenDiminishing),
            _                                   => 0
        };

        if (diminishingCurveId != 0)
            return DB2Manager.GetCurveValueAt(diminishingCurveId, (float)bonusValue);

        return bonusValue;
    }

    private bool CanTitanGrip()
    {
        return _canTitanGrip;
    }

    private void CheckDuelDistance(long currTime)
    {
        if (Duel == null)
            return;

        ObjectGuid duelFlagGuid = PlayerData.DuelArbiter;
        var obj = Location.Map.GetGameObject(duelFlagGuid);

        if (obj == null)
            return;

        if (Duel.OutOfBoundsTime == 0)
        {
            if (Location.IsWithinDistInMap(obj, 50))
                return;

            Duel.OutOfBoundsTime = currTime + 10;
            SendPacket(new DuelOutOfBounds());
        }
        else
        {
            if (Location.IsWithinDistInMap(obj, 40))
            {
                Duel.OutOfBoundsTime = 0;
                SendPacket(new DuelInBounds());
            }
            else if (currTime >= Duel.OutOfBoundsTime)
                DuelComplete(DuelCompleteType.Fled);
        }
    }

    private void CheckTitanGripPenalty()
    {
        if (!CanTitanGrip())
            return;

        var apply = IsUsingTwoHandedWeaponInOneHand();

        if (apply)
        {
            if (!HasAura(_titanGripPenaltySpellId))
                SpellFactory.CastSpell((Unit)null, _titanGripPenaltySpellId, true);
        }
        else
            RemoveAura(_titanGripPenaltySpellId);
    }

    private void GetDodgeFromAgility(double diminishing, double nondiminishing)
    {
        /*// Table for base dodge values
        float[] dodge_base =
        {
            0.037580f, // Warrior
            0.036520f, // Paladin
            -0.054500f, // Hunter
            -0.005900f, // Rogue
            0.031830f, // Priest
            0.036640f, // DK
            0.016750f, // Shaman
            0.034575f, // Mage
            0.020350f, // Warlock
            0.0f,      // ??
            0.049510f  // Druid
        };
        // Crit/agility to dodge/agility coefficient multipliers; 3.2.0 increased required agility by 15%
        float[] crit_to_dodge =
        {
            0.85f/1.15f,    // Warrior
            1.00f/1.15f,    // Paladin
            1.11f/1.15f,    // Hunter
            2.00f/1.15f,    // Rogue
            1.00f/1.15f,    // Priest
            0.85f/1.15f,    // DK
            1.60f/1.15f,    // Shaman
            1.00f/1.15f,    // Mage
            0.97f/1.15f,    // Warlock (?)
            0.0f,           // ??
            2.00f/1.15f     // Druid
        };

        uint level = getLevel();
        uint pclass = (uint)GetClass();

        if (level > CliDB.GtChanceToMeleeCritStorage.GetTableRowCount())
            level = CliDB.GtChanceToMeleeCritStorage.GetTableRowCount() - 1;

        // Dodge per agility is proportional to crit per agility, which is available from DBC files
        var dodgeRatio = CliDB.GtChanceToMeleeCritStorage.EvaluateTable(level - 1, pclass - 1);
        if (dodgeRatio == null || pclass > (int)Class.Max)
            return;

        // @todo research if talents/effects that increase total agility by x% should increase non-diminishing part
        float base_agility = GetCreateStat(Stats.Agility) * GetPctModifierValue(UnitMods(UNIT_MOD_STAT_START + STAT_AGILITY), BASE_PCT);
        float bonus_agility = GetStat(Stats.Agility) - base_agility;

        // calculate diminishing (green in char screen) and non-diminishing (white) contribution
        diminishing = 100.0f * bonus_agility * dodgeRatio.Value * crit_to_dodge[(int)pclass - 1];
        nondiminishing = 100.0f * (dodge_base[(int)pclass - 1] + base_agility * dodgeRatio.Value * crit_to_dodge[pclass - 1]);
        */
    }

    private float GetRatingMultiplier(CombatRating cr)
    {
        var rating = CliDB.CombatRatingsGameTable.GetRow(Level);

        if (rating == null)
            return 1.0f;

        var value = GetGameTableColumnForCombatRating(rating, cr);

        if (value == 0)
            return 1.0f; // By default use minimum coefficient (not must be called)

        return 1.0f / value;
    }

    private void InitPvP()
    {
        // pvp Id should stay after relog
        if (HasPlayerFlag(PlayerFlags.InPVP))
            UpdatePvP(true, true);
    }

    private bool IsTwoHandUsed()
    {
        var mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

        if (mainItem == null)
            return false;

        var itemTemplate = mainItem.Template;

        return (itemTemplate.InventoryType == InventoryType.Weapon2Hand && !CanTitanGrip()) ||
               itemTemplate.InventoryType == InventoryType.Ranged ||
               (itemTemplate.InventoryType == InventoryType.RangedRight && itemTemplate.Class == ItemClass.Weapon && (ItemSubClassWeapon)itemTemplate.SubClass != ItemSubClassWeapon.Wand);
    }

    private bool IsUsingTwoHandedWeaponInOneHand()
    {
        var offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

        if (offItem != null && offItem.Template.InventoryType == InventoryType.Weapon2Hand)
            return true;

        var mainItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

        if (mainItem == null || mainItem.Template.InventoryType == InventoryType.Weapon2Hand)
            return false;

        return offItem != null;
    }

    private void SetDuelTeam(uint duelTeam)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.DuelTeam), duelTeam);
    }

    private void SetRegularAttackTime()
    {
        for (WeaponAttackType weaponAttackType = 0; weaponAttackType < WeaponAttackType.Max; ++weaponAttackType)
        {
            var tmpitem = GetWeaponForAttack(weaponAttackType, true);

            if (tmpitem is { IsBroken: false })
            {
                var proto = tmpitem.Template;

                if (proto.Delay != 0)
                    SetBaseAttackTime(weaponAttackType, proto.Delay);
            }
            else
                SetBaseAttackTime(weaponAttackType, SharedConst.BaseAttackTime); // If there is no weapon reset attack time to base (might have been changed from forms)
        }
    }

    private void UpdateAfkReport(long currTime)
    {
        if (_bgData.BgAfkReportedTimer <= currTime)
        {
            _bgData.BgAfkReportedCount = 0;
            _bgData.BgAfkReportedTimer = currTime + 5 * Time.MINUTE;
        }
    }

    private void UpdateDuelFlag(long currTime)
    {
        if (Duel is { State: DuelState.Countdown } && Duel.StartTime <= currTime)
        {
            ScriptManager.ForEach<IPlayerOnDuelStart>(p => p.OnDuelStart(this, Duel.Opponent));

            SetDuelTeam(1);
            Duel.Opponent.SetDuelTeam(2);

            Duel.State = DuelState.InProgress;
            Duel.Opponent.Duel.State = DuelState.InProgress;
        }
    }
}