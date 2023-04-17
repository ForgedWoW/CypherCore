// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Spells.Skills;

public class SkillExtraItems
{
    private readonly Dictionary<uint, SkillExtraItemEntry> _skillExtraItemStorage = new();
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;

    public SkillExtraItems(WorldDatabase worldDatabase, SpellManager spellManager)
    {
        _worldDatabase = worldDatabase;
        _spellManager = spellManager;
    }

    public bool CanCreateExtraItems(Player player, uint spellId, ref double additionalChance, ref byte additionalMax)
    {
        // get the info for the specified spell
        if (!_skillExtraItemStorage.TryGetValue(spellId, out var specEntry))
            return false;

        // the player doesn't have the required specialization, return false
        if (!player.HasSpell(specEntry.RequiredSpecialization))
            return false;

        // set the arguments to the appropriate values
        additionalChance = specEntry.AdditionalCreateChance;
        additionalMax = specEntry.AdditionalMaxNum;

        // enable extra item creation
        return true;
    }

    // loads the extra item creation info from DB
    public void LoadSkillExtraItemTable()
    {
        var oldMSTime = Time.MSTime;

        _skillExtraItemStorage.Clear(); // need for reload

        //                                             0               1                       2                    3
        var result = _worldDatabase.Query("SELECT spellId, requiredSpecialization, additionalCreateChance, additionalMaxNum FROM skill_extra_item_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spell specialization definitions. DB table `skill_extra_item_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var spellId = result.Read<uint>(0);

            if (!_spellManager.HasSpellInfo(spellId))
            {
                Log.Logger.Error("Skill specialization {0} has non-existent spell id in `skill_extra_item_template`!", spellId);

                continue;
            }

            var requiredSpecialization = result.Read<uint>(1);

            if (!_spellManager.HasSpellInfo(requiredSpecialization))
            {
                Log.Logger.Error("Skill specialization {0} have not existed required specialization spell id {1} in `skill_extra_item_template`!", spellId, requiredSpecialization);

                continue;
            }

            var additionalCreateChance = result.Read<double>(2);

            if (additionalCreateChance <= 0.0f)
            {
                Log.Logger.Error("Skill specialization {0} has too low additional create chance in `skill_extra_item_template`!", spellId);

                continue;
            }

            var additionalMaxNum = result.Read<byte>(3);

            if (additionalMaxNum == 0)
            {
                Log.Logger.Error("Skill specialization {0} has 0 max number of extra items in `skill_extra_item_template`!", spellId);

                continue;
            }

            SkillExtraItemEntry skillExtraItemEntry = new()
            {
                RequiredSpecialization = requiredSpecialization,
                AdditionalCreateChance = additionalCreateChance,
                AdditionalMaxNum = additionalMaxNum
            };

            _skillExtraItemStorage[spellId] = skillExtraItemEntry;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} spell specialization definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}