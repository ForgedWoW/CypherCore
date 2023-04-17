// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Spells.Skills;

public class SkillPerfectItems
{
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, SkillPerfectItemEntry> _skillPerfectItemStorage = new();
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;

    public SkillPerfectItems(WorldDatabase worldDatabase, SpellManager spellManager, GameObjectManager objectManager)
    {
        _worldDatabase = worldDatabase;
        _spellManager = spellManager;
        _objectManager = objectManager;
    }

    public bool CanCreatePerfectItem(Player player, uint spellId, ref double perfectCreateChance, ref uint perfectItemType)
    {
        var entry = _skillPerfectItemStorage.LookupByKey(spellId);

        // no entry in DB means no perfection proc possible
        if (entry == null)
            return false;

        // if you don't have the spell needed, then no procs for you
        if (!player.HasSpell(entry.RequiredSpecialization))
            return false;

        // set values as appropriate
        perfectCreateChance = entry.PerfectCreateChance;
        perfectItemType = entry.PerfectItemType;

        // and tell the caller to start rolling the dice
        return true;
    }

    // loads the perfection proc info from DB
    public void LoadSkillPerfectItemTable()
    {
        var oldMSTime = Time.MSTime;

        _skillPerfectItemStorage.Clear(); // reload capability

        //                                                  0               1                      2                  3
        var result = _worldDatabase.Query("SELECT spellId, requiredSpecialization, perfectCreateChance, perfectItemType FROM skill_perfect_item_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spell perfection definitions. DB table `skill_perfect_item_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var spellId = result.Read<uint>(0);

            if (!_spellManager.HasSpellInfo(spellId))
            {
                Log.Logger.Error("Skill perfection data for spell {0} has non-existent spell id in `skill_perfect_item_template`!", spellId);

                continue;
            }

            var requiredSpecialization = result.Read<uint>(1);

            if (!_spellManager.HasSpellInfo(requiredSpecialization))
            {
                Log.Logger.Error("Skill perfection data for spell {0} has non-existent required specialization spell id {1} in `skill_perfect_item_template`!", spellId, requiredSpecialization);

                continue;
            }

            var perfectCreateChance = result.Read<double>(2);

            if (perfectCreateChance <= 0.0f)
            {
                Log.Logger.Error("Skill perfection data for spell {0} has impossibly low proc chance in `skill_perfect_item_template`!", spellId);

                continue;
            }

            var perfectItemType = result.Read<uint>(3);

            if (_objectManager.GetItemTemplate(perfectItemType) == null)
            {
                Log.Logger.Error("Skill perfection data for spell {0} references non-existent perfect item id {1} in `skill_perfect_item_template`!", spellId, perfectItemType);

                continue;
            }

            _skillPerfectItemStorage[spellId] = new SkillPerfectItemEntry(requiredSpecialization, perfectCreateChance, perfectItemType);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} spell perfection definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}