// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Spells.Skills;

public class SkillDiscovery
{
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly MultiMap<int, SkillDiscoveryEntry> _skillDiscoveryStorage = new();
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;
    public SkillDiscovery(WorldDatabase worldDatabase, SpellManager spellManager, CliDB cliDB, IConfiguration configuration)
    {
        _worldDatabase = worldDatabase;
        _spellManager = spellManager;
        _cliDB = cliDB;
        _configuration = configuration;
    }

    public uint GetExplicitDiscoverySpell(uint spellId, Player player)
    {
        // explicit discovery spell chances (always success if case exist)
        // in this case we have both skill and spell
        if (_skillDiscoveryStorage.TryGetValue((int)spellId, out var tab))
            return 0;

        var bounds = _spellManager.GetSkillLineAbilityMapBounds(spellId);

        if (bounds == null)
            return 0;

        var skillvalue = !bounds.Empty() ? (uint)player.GetSkillValue((SkillType)bounds.FirstOrDefault()!.SkillLine) : 0;

        double fullChance = 0;

        foreach (var itemIter in tab)
            if (itemIter.ReqSkillValue <= skillvalue)
                if (!player.HasSpell(itemIter.SpellId))
                    fullChance += itemIter.Chance;

        var rate = fullChance / 100.0f;
        var roll = RandomHelper.randChance() * rate; // roll now in range 0..full_chance

        foreach (var itemIter in tab)
        {
            if (itemIter.ReqSkillValue > skillvalue)
                continue;

            if (player.HasSpell(itemIter.SpellId))
                continue;

            if (itemIter.Chance > roll)
                return itemIter.SpellId;

            roll -= itemIter.Chance;
        }

        return 0;
    }

    public uint GetSkillDiscoverySpell(uint skillId, uint spellId, Player player)
    {
        var skillvalue = skillId != 0 ? (uint)player.GetSkillValue((SkillType)skillId) : 0;

        // check spell case
        if (_skillDiscoveryStorage.TryGetValue((int)spellId, out var tab))
        {
            foreach (var itemIter in tab)
                if (RandomHelper.randChance(itemIter.Chance * _configuration.GetDefaultValue("Rate:Skill:Discovery", 1.0f)) &&
                    itemIter.ReqSkillValue <= skillvalue &&
                    !player.HasSpell(itemIter.SpellId))
                    return itemIter.SpellId;

            return 0;
        }

        if (skillId == 0)
            return 0;

        // check skill line case
        tab = _skillDiscoveryStorage.LookupByKey(-(int)skillId);

        if (!tab.Empty())
        {
            foreach (var itemIter in tab)
                if (RandomHelper.randChance(itemIter.Chance * _configuration.GetDefaultValue("Rate:Skill:Discovery", 1.0f)) &&
                    itemIter.ReqSkillValue <= skillvalue &&
                    !player.HasSpell(itemIter.SpellId))
                    return itemIter.SpellId;

            return 0;
        }

        return 0;
    }

    public bool HasDiscoveredAllSpells(uint spellId, Player player)
    {
        if (_skillDiscoveryStorage.TryGetValue((int)spellId, out var tab))
            return true;

        foreach (var itemIter in tab)
            if (!player.HasSpell(itemIter.SpellId))
                return false;

        return true;
    }

    public bool HasDiscoveredAnySpell(uint spellId, Player player)
    {
        if (_skillDiscoveryStorage.TryGetValue((int)spellId, out var tab))
            return false;

        foreach (var itemIter in tab)
            if (player.HasSpell(itemIter.SpellId))
                return true;

        return false;
    }

    public void LoadSkillDiscoveryTable()
    {
        var oldMsTime = Time.MSTime;

        _skillDiscoveryStorage.Clear(); // need for reload

        //                                                0        1         2              3
        var result = _worldDatabase.Query("SELECT spellId, reqSpell, reqSkillValue, chance FROM skill_discovery_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 skill discovery definitions. DB table `skill_discovery_template` is empty.");

            return;
        }

        uint count = 0;

        StringBuilder ssNonDiscoverableEntries = new();
        List<uint> reportedReqSpells = new();

        do
        {
            var spellId = result.Read<uint>(0);
            var reqSkillOrSpell = result.Read<int>(1);
            var reqSkillValue = result.Read<uint>(2);
            var chance = result.Read<double>(3);

            if (chance <= 0) // chance
            {
                ssNonDiscoverableEntries.Append($"spellId = {spellId} reqSkillOrSpell = {reqSkillOrSpell} reqSkillValue = {reqSkillValue} chance = {chance} (chance problem)\n");

                continue;
            }

            if (reqSkillOrSpell > 0) // spell case
            {
                var absReqSkillOrSpell = (uint)reqSkillOrSpell;
                var reqSpellInfo = _spellManager.GetSpellInfo(absReqSkillOrSpell);

                if (reqSpellInfo == null)
                {
                    if (!reportedReqSpells.Contains(absReqSkillOrSpell))
                    {
                        Log.Logger.Error("Spell (ID: {0}) have not existed spell (ID: {1}) in `reqSpell` field in `skill_discovery_template` table", spellId, reqSkillOrSpell);
                        reportedReqSpells.Add(absReqSkillOrSpell);
                    }

                    continue;
                }

                // mechanic discovery
                if (reqSpellInfo.Mechanic != Mechanics.Discovery &&
                    // explicit discovery ability
                    !reqSpellInfo.IsExplicitDiscovery)
                {
                    if (!reportedReqSpells.Contains(absReqSkillOrSpell))
                    {
                        Log.Logger.Error("Spell (ID: {0}) not have MECHANIC_DISCOVERY (28) value in Mechanic field in spell.dbc" +
                                         " and not 100%% chance random discovery ability but listed for spellId {1} (and maybe more) in `skill_discovery_template` table",
                                         absReqSkillOrSpell,
                                         spellId);

                        reportedReqSpells.Add(absReqSkillOrSpell);
                    }

                    continue;
                }

                _skillDiscoveryStorage.Add(reqSkillOrSpell, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
            }
            else if (reqSkillOrSpell == 0) // skill case
            {
                var bounds = _spellManager.GetSkillLineAbilityMapBounds(spellId);

                if (bounds.Empty())
                {
                    Log.Logger.Error("Spell (ID: {0}) not listed in `SkillLineAbility.dbc` but listed with `reqSpell`=0 in `skill_discovery_template` table", spellId);

                    continue;
                }

                foreach (var spellIdx in bounds)
                    _skillDiscoveryStorage.Add(-spellIdx.SkillLine, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
            }
            else
            {
                Log.Logger.Error("Spell (ID: {0}) have negative value in `reqSpell` field in `skill_discovery_template` table", spellId);

                continue;
            }

            ++count;
        } while (result.NextRow());

        if (ssNonDiscoverableEntries.Length != 0)
            Log.Logger.Error("Some items can't be successfully discovered: have in chance field value < 0.000001 in `skill_discovery_template` DB table . List:\n{0}", ssNonDiscoverableEntries.ToString());

        // report about empty data for explicit discovery spells
        foreach (var spellNameEntry in _cliDB.SpellNameStorage.Values)
        {
            var spellEntry = _spellManager.GetSpellInfo(spellNameEntry.Id);

            if (spellEntry == null)
                continue;

            // skip not explicit discovery spells
            if (!spellEntry.IsExplicitDiscovery)
                continue;

            if (!_skillDiscoveryStorage.ContainsKey((int)spellEntry.Id))
                Log.Logger.Error("Spell (ID: {0}) is 100% chance random discovery ability but not have data in `skill_discovery_template` table", spellEntry.Id);
        }

        Log.Logger.Information("Loaded {0} skill discovery definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMsTime));
    }
}