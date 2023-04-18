// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Spells;

public class SpellHistory
{
    private readonly MultiMap<uint, ChargeEntry> _categoryCharges = new();
    private readonly LoopSafeDictionary<uint, CooldownEntry> _categoryCooldowns = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly Dictionary<uint, DateTime> _globalCooldowns = new();
    private readonly Unit _owner;
    private readonly DateTime[] _schoolLockouts = new DateTime[(int)SpellSchools.Max];
    private readonly LoopSafeDictionary<uint, CooldownEntry> _spellCooldowns = new();
    private readonly WorldManager _worldManager;
    private Dictionary<uint, CooldownEntry> _spellCooldownsBeforeDuel = new();

    public SpellHistory(Unit owner)
    {
        _owner = owner;
        _characterDatabase = owner.ClassFactory.Resolve<CharacterDatabase>();
        _worldManager = owner.ClassFactory.Resolve<WorldManager>();
    }

    public HashSet<uint> SpellsOnCooldown => _spellCooldowns.Keys.ToHashSet();

    public void AddCooldown<T>(T spellId, uint itemId, TimeSpan cooldownDuration) where T : struct, Enum
    {
        AddCooldown(Convert.ToUInt32(spellId), itemId, cooldownDuration);
    }

    public void AddCooldown(uint spellId, uint itemId, TimeSpan cooldownDuration)
    {
        var now = GameTime.SystemTime;
        AddCooldown(spellId, itemId, now + cooldownDuration, 0, now);
    }

    public void AddCooldown(uint spellId, uint itemId, DateTime cooldownEnd, uint categoryId, DateTime categoryEnd, bool onHold = false)
    {
        CooldownEntry cooldownEntry = new();

        // scripts can start multiple cooldowns for a given spell, only store the longest one
        if (cooldownEnd > cooldownEntry.CooldownEnd || categoryEnd > cooldownEntry.CategoryEnd || onHold)
        {
            cooldownEntry.SpellId = spellId;
            cooldownEntry.CooldownEnd = cooldownEnd;
            cooldownEntry.ItemId = itemId;
            cooldownEntry.CategoryId = categoryId;
            cooldownEntry.CategoryEnd = categoryEnd;
            cooldownEntry.OnHold = onHold;
            _spellCooldowns[spellId] = cooldownEntry;

            if (categoryId != 0)
                _categoryCooldowns[categoryId] = cooldownEntry;
        }
    }

    public void AddGlobalCooldown(SpellInfo spellInfo, TimeSpan durationMs)
    {
        _globalCooldowns[spellInfo.StartRecoveryCategory] = GameTime.SystemTime + durationMs;
    }

    public void CancelGlobalCooldown(SpellInfo spellInfo)
    {
        _globalCooldowns[spellInfo.StartRecoveryCategory] = new DateTime();
    }

    public bool ConsumeCharge(uint chargeCategoryId)
    {
        if (!_owner.CliDB.SpellCategoryStorage.ContainsKey(chargeCategoryId))
            return false;

        var chargeRecovery = GetChargeRecoveryTime(chargeCategoryId);

        if (chargeRecovery <= 0 || GetMaxCharges(chargeCategoryId) <= 0)
            return false;

        var recoveryStart = _categoryCharges.TryGetValue(chargeCategoryId, out var charges) ? GameTime.SystemTime : charges.Last().RechargeEnd;

        var p = GetPlayerOwner();

        if (p != null)
            _owner.ScriptManager.ForEach<IPlayerOnChargeRecoveryTimeStart>(p.Class, c => c.OnChargeRecoveryTimeStart(p, chargeCategoryId, ref chargeRecovery));

        _categoryCharges.Add(chargeCategoryId, new ChargeEntry(recoveryStart, TimeSpan.FromMilliseconds(chargeRecovery)));

        return true;
    }

    public void ForceSendSpellCharge(SpellCategoryRecord chargeCategoryRecord)
    {
        var player = GetPlayerOwner();

        if (player == null || _categoryCharges.ContainsKey(chargeCategoryRecord.Id))
            return;

        var sendSpellCharges = new SendSpellCharges();
        var charges = _categoryCharges[chargeCategoryRecord.Id];

        var now = DateTime.Now;
        var cooldownDuration = (uint)(charges.First().RechargeEnd - now).TotalMilliseconds;

        if (cooldownDuration <= 0)
            return;

        var chargeEntry = new SpellChargeEntry
        {
            Category = chargeCategoryRecord.Id,
            NextRecoveryTime = cooldownDuration,
            ConsumedCharges = (byte)charges.Count()
        };

        sendSpellCharges.Entries.Add(chargeEntry);

        WritePacket(sendSpellCharges);
    }

    public int GetChargeRecoveryTime(uint chargeCategoryId)
    {
        if (!_owner.CliDB.SpellCategoryStorage.TryGetValue(chargeCategoryId, out var chargeCategoryEntry))
            return 0;

        double recoveryTime = chargeCategoryEntry.ChargeRecoveryTime;
        recoveryTime += _owner.GetTotalAuraModifierByMiscValue(AuraType.ChargeRecoveryMod, (int)chargeCategoryId);

        var recoveryTimeF = recoveryTime;
        recoveryTimeF *= _owner.GetTotalAuraMultiplierByMiscValue(AuraType.ChargeRecoveryMultiplier, (int)chargeCategoryId);

        if (_owner.HasAuraType(AuraType.ChargeRecoveryAffectedByHaste))
            recoveryTimeF *= _owner.UnitData.ModSpellHaste;

        if (_owner.HasAuraType(AuraType.ChargeRecoveryAffectedByHasteRegen))
            recoveryTimeF *= _owner.UnitData.ModHasteRegen;

        return (int)Math.Floor(recoveryTimeF);
    }

    public int GetMaxCharges(uint chargeCategoryId)
    {
        if (!_owner.CliDB.SpellCategoryStorage.TryGetValue(chargeCategoryId, out var chargeCategoryEntry))
            return 0;

        uint charges = chargeCategoryEntry.MaxCharges;
        charges += (uint)_owner.GetTotalAuraModifierByMiscValue(AuraType.ModMaxCharges, (int)chargeCategoryId);

        return (int)charges;
    }

    public Player GetPlayerOwner()
    {
        return _owner.CharmerOrOwnerPlayerOrPlayerItself;
    }

    public TimeSpan GetRemainingCategoryCooldown(uint categoryId)
    {
        if (!_categoryCooldowns.TryGetValue(categoryId, out var cooldownEntry))
            return TimeSpan.Zero;

        var end = cooldownEntry.CategoryEnd;

        var now = GameTime.SystemTime;

        if (end < now)
            return TimeSpan.Zero;

        var remaining = end - now;

        return remaining;
    }

    public TimeSpan GetRemainingCategoryCooldown(SpellInfo spellInfo)
    {
        return GetRemainingCategoryCooldown(spellInfo.Category);
    }

    public TimeSpan GetRemainingCooldown(SpellInfo spellInfo)
    {
        DateTime end;

        if (_spellCooldowns.TryGetValue(spellInfo.Id, out var entry))
            end = entry.CooldownEnd;
        else
        {
            if (!_categoryCooldowns.TryGetValue(spellInfo.Category, out var cooldownEntry))
                return TimeSpan.Zero;

            end = cooldownEntry.CategoryEnd;
        }

        var now = GameTime.SystemTime;

        if (end < now)
            return TimeSpan.Zero;

        var remaining = end - now;

        return remaining;
    }

    public void HandleCooldowns(SpellInfo spellInfo, Item item, Spell spell = null)
    {
        HandleCooldowns(spellInfo, item?.Entry ?? 0u, spell);
    }

    public void HandleCooldowns(SpellInfo spellInfo, uint itemId, Spell spell = null)
    {
        if (spell is { IsIgnoringCooldowns: true })
            return;

        if (ConsumeCharge(spellInfo.ChargeCategoryId))
            return;

        var player = _owner.AsPlayer;

        if (player != null)
        {
            // potions start cooldown until exiting combat
            var itemTemplate = _owner.ObjectManager.GetItemTemplate(itemId);

            if (itemTemplate != null)
                if (itemTemplate.IsPotion || spellInfo.IsCooldownStartedOnEvent)
                {
                    player.SetLastPotionId(itemId);

                    return;
                }
        }

        if (spellInfo.IsCooldownStartedOnEvent || spellInfo.IsPassive)
            return;

        StartCooldown(spellInfo, itemId, spell);
    }

    public bool HasCharge(uint chargeCategoryId)
    {
        if (!_owner.CliDB.SpellCategoryStorage.ContainsKey(chargeCategoryId))
            return true;

        // Check if the spell is currently using charges (untalented warlock Dark Soul)
        var maxCharges = GetMaxCharges(chargeCategoryId);

        if (maxCharges <= 0)
            return true;

        var chargeList = _categoryCharges.LookupByKey(chargeCategoryId);

        return chargeList.Empty() || chargeList.Count < maxCharges;
    }

    public bool HasCooldown(uint spellId, uint itemId = 0)
    {
        return HasCooldown(_owner.SpellManager.GetSpellInfo(spellId, _owner.Location.Map.DifficultyID), itemId);
    }

    public bool HasCooldown(SpellInfo spellInfo, uint itemId = 0)
    {
        if (_spellCooldowns.ContainsKey(spellInfo.Id))
            return true;

        if (spellInfo.CooldownAuraSpellId != 0 && _owner.HasAura(spellInfo.CooldownAuraSpellId))
            return true;

        uint category = 0;
        GetCooldownDurations(spellInfo, itemId, ref category);

        if (category == 0)
            category = spellInfo.Category;

        return category != 0 && _categoryCooldowns.ContainsKey(category);
    }

    public bool HasGlobalCooldown(SpellInfo spellInfo)
    {
        return _globalCooldowns.ContainsKey(spellInfo.StartRecoveryCategory) && _globalCooldowns[spellInfo.StartRecoveryCategory] > GameTime.SystemTime;
    }

    public bool IsReady(SpellInfo spellInfo, uint itemId = 0)
    {
        if (spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
            if (IsSchoolLocked(spellInfo.GetSchoolMask()))
                return false;

        if (HasCooldown(spellInfo, itemId))
            return false;

        return HasCharge(spellInfo.ChargeCategoryId);
    }

    public bool IsSchoolLocked(SpellSchoolMask schoolMask)
    {
        var now = GameTime.SystemTime;

        for (var i = 0; i < (int)SpellSchools.Max; ++i)
            if (Convert.ToBoolean((SpellSchoolMask)(1 << i) & schoolMask))
                if (_schoolLockouts[i] > now)
                    return true;

        return false;
    }

    public void LoadFromDb<T>(SQLResult cooldownsResult, SQLResult chargesResult) where T : WorldObject
    {
        if (!cooldownsResult.IsEmpty())
            do
            {
                CooldownEntry cooldownEntry = new()
                {
                    SpellId = cooldownsResult.Read<uint>(0)
                };

                if (!_owner.SpellManager.HasSpellInfo(cooldownEntry.SpellId))
                    continue;

                if (typeof(T) == typeof(Pet))
                {
                    cooldownEntry.CooldownEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(1));
                    cooldownEntry.ItemId = 0;
                    cooldownEntry.CategoryId = cooldownsResult.Read<uint>(2);
                    cooldownEntry.CategoryEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(3));
                }
                else
                {
                    cooldownEntry.CooldownEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(2));
                    cooldownEntry.ItemId = cooldownsResult.Read<uint>(1);
                    cooldownEntry.CategoryId = cooldownsResult.Read<uint>(3);
                    cooldownEntry.CategoryEnd = Time.UnixTimeToDateTime(cooldownsResult.Read<long>(4));
                }

                _spellCooldowns[cooldownEntry.SpellId] = cooldownEntry;

                if (cooldownEntry.CategoryId != 0)
                    _categoryCooldowns[cooldownEntry.CategoryId] = _spellCooldowns[cooldownEntry.SpellId];
            } while (cooldownsResult.NextRow());

        if (chargesResult.IsEmpty())
            return;

        do
        {
            var categoryId = chargesResult.Read<uint>(0);

            if (!_owner.CliDB.SpellCategoryStorage.ContainsKey(categoryId))
                continue;

            ChargeEntry charges;
            charges.RechargeStart = Time.UnixTimeToDateTime(chargesResult.Read<long>(1));
            charges.RechargeEnd = Time.UnixTimeToDateTime(chargesResult.Read<long>(2));
            _categoryCharges.Add(categoryId, charges);
        } while (chargesResult.NextRow());
    }

    public void LockSpellSchool(SpellSchoolMask schoolMask, TimeSpan lockoutTime)
    {
        var now = GameTime.SystemTime;
        var lockoutEnd = now + lockoutTime;

        for (var i = 0; i < (int)SpellSchools.Max; ++i)
            if (Convert.ToBoolean((SpellSchoolMask)(1 << i) & schoolMask))
                _schoolLockouts[i] = lockoutEnd;

        List<uint> knownSpells = new();
        var plrOwner = _owner.AsPlayer;

        if (plrOwner != null)
        {
            foreach (var p in plrOwner.GetSpellMap())
                if (p.Value.State != PlayerSpellState.Removed)
                    knownSpells.Add(p.Key);
        }
        else if (_owner.IsPet)
        {
            var petOwner = _owner.AsPet;

            foreach (var p in petOwner.Spells)
                if (p.Value.State != PetSpellState.Removed)
                    knownSpells.Add(p.Key);
        }
        else
        {
            var creatureOwner = _owner.AsCreature;

            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                if (creatureOwner.Spells[i] != 0)
                    knownSpells.Add(creatureOwner.Spells[i]);
        }

        SpellCooldownPkt spellCooldown = new()
        {
            Caster = _owner.GUID,
            Flags = SpellCooldownFlags.LossOfControlUi
        };

        foreach (var spellId in knownSpells)
        {
            var spellInfo = _owner.SpellManager.GetSpellInfo(spellId, _owner.Location.Map.DifficultyID);

            if (spellInfo.IsCooldownStartedOnEvent)
                continue;

            if (!spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                continue;

            if ((schoolMask & spellInfo.GetSchoolMask()) == 0)
                continue;

            if (GetRemainingCooldown(spellInfo) < lockoutTime)
                AddCooldown(spellId, 0, lockoutEnd, 0, now);

            // always send cooldown, even if it will be shorter than already existing cooldown for LossOfControl UI
            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellId, (uint)lockoutTime.TotalMilliseconds));
        }

        var player = GetPlayerOwner();

        if (player == null)
            return;

        if (!spellCooldown.SpellCooldowns.Empty())
            player.SendPacket(spellCooldown);
    }

    public void ModifyCooldown<T>(T spellId, TimeSpan cooldownMod, bool withoutCategoryCooldown = false) where T : struct, Enum
    {
        ModifyCooldown(Convert.ToUInt32(spellId), cooldownMod, withoutCategoryCooldown);
    }

    public void ModifyCooldown(uint spellId, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
    {
        var spellInfo = _owner.SpellManager.GetSpellInfo(spellId, _owner.Location.Map.DifficultyID);

        if (spellInfo != null)
            ModifyCooldown(spellInfo, cooldownMod, withoutCategoryCooldown);
    }

    public void ModifyCooldown(SpellInfo spellInfo, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
    {
        if (cooldownMod == TimeSpan.Zero)
            return;

        if (GetChargeRecoveryTime(spellInfo.ChargeCategoryId) > 0 && GetMaxCharges(spellInfo.ChargeCategoryId) > 0)
            ModifyChargeRecoveryTime(spellInfo.ChargeCategoryId, cooldownMod);
        else
            ModifySpellCooldown(spellInfo.Id, cooldownMod, withoutCategoryCooldown);
    }

    public void ModifyCoooldowns(Func<CooldownEntry, bool> predicate, TimeSpan cooldownMod, bool withoutCategoryCooldown = false)
    {
        foreach (var cooldownEntry in _spellCooldowns.Values.ToList().Where(cooldownEntry => predicate(cooldownEntry)))
            ModifySpellCooldown(cooldownEntry, cooldownMod, withoutCategoryCooldown);
    }

    public void ModifySpellCooldown(uint spellId, TimeSpan cooldownMod, bool withoutCategoryCooldown)
    {
        if (!_spellCooldowns.TryGetValue(spellId, out var cooldownEntry))
            return;

        ModifySpellCooldown(cooldownEntry, cooldownMod, withoutCategoryCooldown);
    }

    public void ResetAllCharges()
    {
        _categoryCharges.Clear();

        var player = GetPlayerOwner();

        if (player == null)
            return;

        ClearAllSpellCharges clearAllSpellCharges = new()
        {
            IsPet = _owner != player
        };

        player.SendPacket(clearAllSpellCharges);
    }

    public void ResetAllCooldowns()
    {
        var playerOwner = GetPlayerOwner();

        if (playerOwner != null)
        {
            var cooldowns = _spellCooldowns.Keys.ToList();

            SendClearCooldowns(cooldowns);
        }

        _categoryCooldowns.Clear();
        _spellCooldowns.Clear();
    }

    public void ResetCharges(uint chargeCategoryId)
    {
        if (!_categoryCharges.ContainsKey(chargeCategoryId))
            return;

        _categoryCharges.Remove(chargeCategoryId);

        var player = GetPlayerOwner();

        if (player == null)
            return;

        ClearSpellCharges clearSpellCharges = new()
        {
            IsPet = _owner != player,
            Category = chargeCategoryId
        };

        player.SendPacket(clearSpellCharges);
    }

    public void ResetCooldown(uint spellId, bool update = false)
    {
        if (!_spellCooldowns.TryGetValue(spellId, out var entry))
            return;

        if (update)
        {
            var playerOwner = GetPlayerOwner();

            if (playerOwner != null)
            {
                ClearCooldown clearCooldown = new()
                {
                    IsPet = _owner != playerOwner,
                    SpellID = spellId,
                    ClearOnHold = false
                };

                playerOwner.SendPacket(clearCooldown);
            }
        }

        _categoryCooldowns.Remove(entry.CategoryId);
        _spellCooldowns.Remove(spellId);
    }

    public void ResetCooldowns(Func<KeyValuePair<uint, CooldownEntry>, bool> predicate, bool update = false)
    {
        List<uint> resetCooldowns = new();

        foreach (var pair in _spellCooldowns.Where(predicate))
        {
            resetCooldowns.Add(pair.Key);
            ResetCooldown(pair.Key);
        }

        if (update && !resetCooldowns.Empty())
            SendClearCooldowns(resetCooldowns);
    }

    public void RestoreCharge(uint chargeCategoryId)
    {
        if (!_categoryCharges.TryGetValue(chargeCategoryId, out var chargeList))
            return;

        chargeList.RemoveAt(chargeList.Count - 1);

        SendSetSpellCharges(chargeCategoryId, chargeList);

        if (chargeList.Empty())
            _categoryCharges.Remove(chargeCategoryId);
    }

    public void RestoreCooldownStateAfterDuel()
    {
        var player = _owner.AsPlayer;

        if (player == null)
            return;

        // add all profession CDs created while in duel (if any)
        foreach (var c in _spellCooldowns)
        {
            var spellInfo = _owner.SpellManager.GetSpellInfo(c.Key);

            if (spellInfo.RecoveryTime > 10 * Time.MINUTE * Time.IN_MILLISECONDS || spellInfo.CategoryRecoveryTime > 10 * Time.MINUTE * Time.IN_MILLISECONDS)
                _spellCooldownsBeforeDuel[c.Key] = _spellCooldowns[c.Key];
        }

        // check for spell with onHold active before and during the duel
        foreach (var pair in _spellCooldownsBeforeDuel)
            if (!pair.Value.OnHold && _spellCooldowns.ContainsKey(pair.Key) && !_spellCooldowns[pair.Key].OnHold)
                _spellCooldowns[pair.Key] = _spellCooldownsBeforeDuel[pair.Key];

        // update the client: restore old cooldowns
        SpellCooldownPkt spellCooldown = new()
        {
            Caster = _owner.GUID,
            Flags = SpellCooldownFlags.IncludeEventCooldowns
        };

        foreach (var c in _spellCooldowns)
        {
            var now = GameTime.SystemTime;
            var cooldownDuration = c.Value.CooldownEnd > now ? (uint)(c.Value.CooldownEnd - now).TotalMilliseconds : 0;

            // cooldownDuration must be between 0 and 10 minutes in order to avoid any visual bugs
            if (cooldownDuration is <= 0 or > 10 * Time.MINUTE * Time.IN_MILLISECONDS || c.Value.OnHold)
                continue;

            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(c.Key, cooldownDuration));
        }

        player.SendPacket(spellCooldown);
    }

    public void SaveCooldownStateBeforeDuel()
    {
        _spellCooldownsBeforeDuel = _spellCooldowns;
    }

    public void SaveToDb<T>(SQLTransaction trans) where T : WorldObject
    {
        PreparedStatement stmt;

        if (typeof(T) == typeof(Pet))
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
            stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
            stmt.AddValue(0, _owner.GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            byte index;

            foreach (var pair in _spellCooldowns)
                if (!pair.Value.OnHold)
                {
                    index = 0;
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL_COOLDOWN);
                    stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                    stmt.AddValue(index++, pair.Value.CategoryId);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                    trans.Append(stmt);
                }

            foreach (var pair in _categoryCharges.KeyValueList)
            {
                index = 0;
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL_CHARGES);
                stmt.AddValue(index++, _owner.GetCharmInfo().GetPetNumber());
                stmt.AddValue(index++, pair.Key);
                stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                trans.Append(stmt);
            }
        }
        else
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
            stmt.AddValue(0, _owner.GUID.Counter);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_CHARGES);
            stmt.AddValue(0, _owner.GUID.Counter);
            trans.Append(stmt);

            byte index;

            foreach (var pair in _spellCooldowns)
                if (!pair.Value.OnHold)
                {
                    index = 0;
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_COOLDOWN);
                    stmt.AddValue(index++, _owner.GUID.Counter);
                    stmt.AddValue(index++, pair.Key);
                    stmt.AddValue(index++, pair.Value.ItemId);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CooldownEnd));
                    stmt.AddValue(index++, pair.Value.CategoryId);
                    stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.CategoryEnd));
                    trans.Append(stmt);
                }

            foreach (var pair in _categoryCharges.KeyValueList)
            {
                index = 0;
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_CHARGES);
                stmt.AddValue(index++, _owner.GUID.Counter);
                stmt.AddValue(index++, pair.Key);
                stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeStart));
                stmt.AddValue(index++, Time.DateTimeToUnixTime(pair.Value.RechargeEnd));
                trans.Append(stmt);
            }
        }
    }

    public void SendClearCooldowns(List<uint> cooldowns)
    {
        var playerOwner = GetPlayerOwner();

        if (playerOwner == null)
            return;

        ClearCooldowns clearCooldowns = new()
        {
            IsPet = _owner != playerOwner,
            SpellID = cooldowns
        };

        playerOwner.SendPacket(clearCooldowns);
    }

    public void SendCooldownEvent(SpellInfo spellInfo, uint itemId = 0, Spell spell = null, bool startCooldown = true)
    {
        var player = GetPlayerOwner();

        if (player != null)
        {
            var category = spellInfo.Category;
            GetCooldownDurations(spellInfo, itemId, ref category);

            var categoryEntry = _categoryCooldowns.LookupByKey(category);

            if (categoryEntry != null && categoryEntry.SpellId != spellInfo.Id)
            {
                player.SendPacket(new CooldownEvent(player != _owner, categoryEntry.SpellId));

                if (startCooldown)
                    StartCooldown(_owner.SpellManager.GetSpellInfo(categoryEntry.SpellId, _owner.Location.Map.DifficultyID), itemId, spell);
            }

            player.SendPacket(new CooldownEvent(player != _owner, spellInfo.Id));
        }

        // start cooldowns at server side, if any
        if (startCooldown)
            StartCooldown(spellInfo, itemId, spell);
    }

    public void StartCooldown(SpellInfo spellInfo, uint itemId, Spell spell = null, bool onHold = false, TimeSpan? forcedCooldown = null)
    {
        // init cooldown values
        uint categoryId = 0;
        var cooldown = TimeSpan.Zero;
        var categoryCooldown = TimeSpan.Zero;

        var curTime = GameTime.SystemTime;
        DateTime catrecTime;
        DateTime recTime;
        var needsCooldownPacket = false;

        if (!forcedCooldown.HasValue)
            GetCooldownDurations(spellInfo, itemId, ref cooldown, ref categoryId, ref categoryCooldown);
        else
            cooldown = forcedCooldown.Value;

        // overwrite time for selected category
        if (onHold)
        {
            // use +MONTH as infinite cooldown marker
            catrecTime = categoryCooldown > TimeSpan.Zero ? curTime + PlayerConst.InfinityCooldownDelay : curTime;
            recTime = cooldown > TimeSpan.Zero ? curTime + PlayerConst.InfinityCooldownDelay : catrecTime;
        }
        else
        {
            if (!forcedCooldown.HasValue)
            {
                // Now we have cooldown data (if found any), time to apply mods
                var modOwner = _owner.SpellModOwner;

                if (modOwner != null)
                {
                    void ApplySpellMod(ref TimeSpan value)
                    {
                        var intValue = (int)value.TotalMilliseconds;
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.Cooldown, ref intValue, spell);
                        value = TimeSpan.FromMilliseconds(intValue);
                    }

                    if (cooldown >= TimeSpan.Zero)
                        ApplySpellMod(ref cooldown);

                    if (categoryCooldown >= TimeSpan.Zero && !spellInfo.HasAttribute(SpellAttr6.NoCategoryCooldownMods))
                        ApplySpellMod(ref categoryCooldown);
                }

                if (_owner.HasAuraTypeWithAffectMask(AuraType.ModSpellCooldownByHaste, spellInfo))
                {
                    cooldown = TimeSpan.FromMilliseconds(cooldown.TotalMilliseconds * _owner.UnitData.ModSpellHaste);
                    categoryCooldown = TimeSpan.FromMilliseconds(categoryCooldown.TotalMilliseconds * _owner.UnitData.ModSpellHaste);
                }

                if (_owner.HasAuraTypeWithAffectMask(AuraType.ModCooldownByHasteRegen, spellInfo))
                {
                    cooldown = TimeSpan.FromMilliseconds(cooldown.TotalMilliseconds * _owner.UnitData.ModHasteRegen);
                    categoryCooldown = TimeSpan.FromMilliseconds(categoryCooldown.TotalMilliseconds * _owner.UnitData.ModHasteRegen);
                }

                var cooldownMod = _owner.GetTotalAuraModifier(AuraType.ModCooldown);

                if (cooldownMod != 0)
                {
                    // Apply SPELL_AURA_MOD_COOLDOWN only to own spells
                    var playerOwner = GetPlayerOwner();

                    if (playerOwner == null || playerOwner.HasSpell(spellInfo.Id))
                    {
                        needsCooldownPacket = true;
                        cooldown += TimeSpan.FromMilliseconds(cooldownMod); // SPELL_AURA_MOD_COOLDOWN does not affect category cooldows, verified with shaman shocks
                    }
                }

                // Apply SPELL_AURA_MOD_SPELL_CATEGORY_COOLDOWN modifiers
                // Note: This aura applies its modifiers to all cooldowns of spells with set category, not to category cooldown only
                if (categoryId != 0)
                {
                    var categoryModifier = _owner.GetTotalAuraModifierByMiscValue(AuraType.ModSpellCategoryCooldown, (int)categoryId);

                    if (categoryModifier != 0)
                    {
                        if (cooldown > TimeSpan.Zero)
                            cooldown += TimeSpan.FromMilliseconds(categoryModifier);

                        if (categoryCooldown > TimeSpan.Zero)
                            categoryCooldown += TimeSpan.FromMilliseconds(categoryModifier);
                    }

                    var categoryEntry = _owner.CliDB.SpellCategoryStorage.LookupByKey(categoryId);

                    if (categoryEntry.Flags.HasAnyFlag(SpellCategoryFlags.CooldownExpiresAtDailyReset))
                        categoryCooldown = Time.UnixTimeToDateTime(_worldManager.NextDailyQuestsResetTime) - GameTime.SystemTime;
                }
            }
            else
                needsCooldownPacket = true;

            // replace negative cooldowns by 0
            if (cooldown < TimeSpan.Zero)
                cooldown = TimeSpan.Zero;

            if (categoryCooldown < TimeSpan.Zero)
                categoryCooldown = TimeSpan.Zero;

            // no cooldown after applying spell mods
            if (cooldown == TimeSpan.Zero && categoryCooldown == TimeSpan.Zero)
                return;

            catrecTime = categoryCooldown != TimeSpan.Zero ? curTime + categoryCooldown : curTime;
            recTime = cooldown != TimeSpan.Zero ? curTime + cooldown : catrecTime;
        }

        // self spell cooldown
        if (recTime != curTime)
        {
            var playerOwner = GetPlayerOwner();

            if (playerOwner != null)
                _owner.ScriptManager.ForEach<IPlayerOnCooldownStart>(playerOwner.Class, c => c.OnCooldownStart(playerOwner, spellInfo, itemId, categoryId, cooldown, ref recTime, ref catrecTime, ref onHold));

            AddCooldown(spellInfo.Id, itemId, recTime, categoryId, catrecTime, onHold);

            if (playerOwner == null)
                return;

            if (!needsCooldownPacket)
                return;

            SpellCooldownPkt spellCooldown = new()
            {
                Caster = _owner.GUID,
                Flags = SpellCooldownFlags.None
            };

            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(spellInfo.Id, (uint)cooldown.TotalMilliseconds));
            playerOwner.SendPacket(spellCooldown);
        }
    }

    public void Update()
    {
        var now = GameTime.SystemTime;

        foreach (var pair in _categoryCooldowns.Where(pair => pair.Value.CategoryEnd < now))
            _categoryCooldowns.QueueRemove(pair.Key);

        _categoryCooldowns.ExecuteRemove();

        foreach (var pair in _spellCooldowns.Where(pair => pair.Value.CooldownEnd < now))
        {
            _categoryCooldowns.Remove(pair.Value.CategoryId);
            _spellCooldowns.QueueRemove(pair.Key);
        }

        _spellCooldowns.ExecuteRemove();

        _categoryCharges.RemoveIfMatching((pair) => pair.Value.RechargeEnd <= now);
    }

    public void WritePacket(SendSpellHistory sendSpellHistory)
    {
        var now = GameTime.SystemTime;

        foreach (var p in _spellCooldowns)
        {
            SpellHistoryEntry historyEntry = new()
            {
                SpellID = p.Key,
                ItemID = p.Value.ItemId
            };

            if (p.Value.OnHold)
                historyEntry.OnHold = true;
            else
            {
                var cooldownDuration = p.Value.CooldownEnd - now;

                if (cooldownDuration.TotalMilliseconds <= 0)
                    continue;

                var categoryDuration = p.Value.CategoryEnd - now;

                if (categoryDuration.TotalMilliseconds > 0)
                {
                    historyEntry.Category = p.Value.CategoryId;
                    historyEntry.CategoryRecoveryTime = (int)categoryDuration.TotalMilliseconds;
                }

                if (cooldownDuration > categoryDuration)
                    historyEntry.RecoveryTime = (int)cooldownDuration.TotalMilliseconds;
            }

            sendSpellHistory.Entries.Add(historyEntry);
        }
    }

    public void WritePacket(SendSpellCharges sendSpellCharges)
    {
        var now = GameTime.SystemTime;

        foreach (var key in _categoryCharges.Keys)
        {
            var list = _categoryCharges[key];

            if (list.Empty())
                continue;

            var cooldownDuration = list.FirstOrDefault().RechargeEnd - now;

            if (cooldownDuration.TotalMilliseconds <= 0)
                continue;

            SpellChargeEntry chargeEntry = new()
            {
                Category = key,
                NextRecoveryTime = (uint)cooldownDuration.TotalMilliseconds,
                ConsumedCharges = (byte)list.Count
            };

            sendSpellCharges.Entries.Add(chargeEntry);
        }
    }

    public void WritePacket(PetSpells petSpells)
    {
        var now = GameTime.SystemTime;

        foreach (var pair in _spellCooldowns)
        {
            PetSpellCooldown petSpellCooldown = new()
            {
                SpellID = pair.Key,
                Category = (ushort)pair.Value.CategoryId
            };

            if (!pair.Value.OnHold)
            {
                var cooldownDuration = pair.Value.CooldownEnd - now;

                if (cooldownDuration.TotalMilliseconds <= 0)
                    continue;

                petSpellCooldown.Duration = (uint)cooldownDuration.TotalMilliseconds;
                var categoryDuration = pair.Value.CategoryEnd - now;

                if (categoryDuration.TotalMilliseconds > 0)
                    petSpellCooldown.CategoryDuration = (uint)categoryDuration.TotalMilliseconds;
            }
            else
                petSpellCooldown.CategoryDuration = 0x80000000;

            petSpells.Cooldowns.Add(petSpellCooldown);
        }

        foreach (var key in _categoryCharges.Keys)
        {
            var list = _categoryCharges[key];

            if (list.Empty())
                continue;

            var cooldownDuration = list.FirstOrDefault().RechargeEnd - now;

            if (cooldownDuration.TotalMilliseconds <= 0)
                continue;

            PetSpellHistory petChargeEntry = new()
            {
                CategoryID = key,
                RecoveryTime = (uint)cooldownDuration.TotalMilliseconds,
                ConsumedCharges = (sbyte)list.Count
            };

            petSpells.SpellHistory.Add(petChargeEntry);
        }
    }

    private void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref uint categoryId)
    {
        var notUsed = TimeSpan.Zero;
        GetCooldownDurations(spellInfo, itemId, ref notUsed, ref categoryId, ref notUsed);
    }

    private void GetCooldownDurations(SpellInfo spellInfo, uint itemId, ref TimeSpan cooldown, ref uint categoryId, ref TimeSpan categoryCooldown)
    {
        var tmpCooldown = TimeSpan.MinValue;
        uint tmpCategoryId = 0;
        var tmpCategoryCooldown = TimeSpan.MinValue;

        // cooldown information stored in ItemEffect.db2, overriding normal cooldown and category
        if (itemId != 0)
        {
            var proto = _owner.ObjectManager.GetItemTemplate(itemId);

            if (proto != null)
                foreach (var itemEffect in proto.Effects.Where(itemEffect => itemEffect.SpellID == spellInfo.Id))
                {
                    tmpCooldown = TimeSpan.FromMilliseconds(itemEffect.CoolDownMSec);
                    tmpCategoryId = itemEffect.SpellCategoryID;
                    tmpCategoryCooldown = TimeSpan.FromMilliseconds(itemEffect.CategoryCoolDownMSec);

                    break;
                }
        }

        // if no cooldown found above then base at DBC data
        if (tmpCooldown < TimeSpan.Zero && tmpCategoryCooldown < TimeSpan.Zero)
        {
            tmpCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);
            tmpCategoryId = spellInfo.Category;
            tmpCategoryCooldown = TimeSpan.FromMilliseconds(spellInfo.CategoryRecoveryTime);
        }

        cooldown = tmpCooldown;
        categoryId = tmpCategoryId;
        categoryCooldown = tmpCategoryCooldown;
    }

    private void ModifyChargeRecoveryTime(uint chargeCategoryId, TimeSpan cooldownMod)
    {
        if (!_owner.CliDB.SpellCategoryStorage.ContainsKey(chargeCategoryId))
            return;

        if (_categoryCharges.TryGetValue(chargeCategoryId, out var chargeList))
            return;

        var now = GameTime.SystemTime;

        for (var i = 0; i < chargeList.Count; ++i)
        {
            var entry = chargeList[i];
            entry.RechargeStart += cooldownMod;
            entry.RechargeEnd += cooldownMod;
        }

        while (!chargeList.Empty() && chargeList[0].RechargeEnd < now)
            chargeList.RemoveAt(0);

        SendSetSpellCharges(chargeCategoryId, chargeList);
    }

    private void ModifySpellCooldown(CooldownEntry cooldownEntry, TimeSpan cooldownMod, bool withoutCategoryCooldown)
    {
        var now = GameTime.SystemTime;

        cooldownEntry.CooldownEnd += cooldownMod;

        if (cooldownEntry.CategoryId != 0)
        {
            if (!withoutCategoryCooldown)
                cooldownEntry.CategoryEnd += cooldownMod;

            // Because category cooldown existence is tied to regular cooldown, we cannot allow a situation where regular cooldown is shorter than category
            if (cooldownEntry.CooldownEnd < cooldownEntry.CategoryEnd)
                cooldownEntry.CooldownEnd = cooldownEntry.CategoryEnd;
        }

        var playerOwner = GetPlayerOwner();

        if (playerOwner != null)
        {
            ModifyCooldown modifyCooldown = new()
            {
                IsPet = _owner != playerOwner,
                SpellID = cooldownEntry.SpellId,
                DeltaTime = (int)cooldownMod.TotalMilliseconds,
                WithoutCategoryCooldown = withoutCategoryCooldown
            };

            playerOwner.SendPacket(modifyCooldown);
        }

        if (cooldownEntry.CooldownEnd > now)
            return;

        if (playerOwner != null)
            _owner.ScriptManager.ForEach<IPlayerOnCooldownEnd>(playerOwner.Class, c => c.OnCooldownEnd(playerOwner, _owner.SpellManager.GetSpellInfo(cooldownEntry.SpellId), cooldownEntry.ItemId, cooldownEntry.CategoryId));

        _categoryCooldowns.Remove(cooldownEntry.CategoryId);
        _spellCooldowns.Remove(cooldownEntry.SpellId);
    }

    private void SendSetSpellCharges(uint chargeCategoryId, List<ChargeEntry> chargeCollection)
    {
        var player = GetPlayerOwner();

        if (player == null)
            return;

        SetSpellCharges setSpellCharges = new()
        {
            Category = chargeCategoryId
        };

        if (!chargeCollection.Empty())
            setSpellCharges.NextRecoveryTime = (uint)(chargeCollection[0].RechargeEnd - DateTime.Now).TotalMilliseconds;

        setSpellCharges.ConsumedCharges = (byte)chargeCollection.Count;
        setSpellCharges.IsPet = player != _owner;
        player.SendPacket(setSpellCharges);
    }

    public struct ChargeEntry
    {
        public DateTime RechargeEnd;

        public DateTime RechargeStart;

        public ChargeEntry(DateTime startTime, TimeSpan rechargeTime)
        {
            RechargeStart = startTime;
            RechargeEnd = startTime + rechargeTime;
        }
    }

    public class CooldownEntry
    {
        public DateTime CategoryEnd { get; set; }
        public uint CategoryId { get; set; }
        public DateTime CooldownEnd { get; set; }
        public uint ItemId { get; set; }
        public bool OnHold { get; set; }
        public uint SpellId { get; set; }
    }
}