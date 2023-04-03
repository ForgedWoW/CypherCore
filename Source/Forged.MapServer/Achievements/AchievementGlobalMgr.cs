// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Achievements;

public class AchievementGlobalMgr
{
    // store achievements by referenced achievement id to speed up lookup
    private readonly MultiMap<uint, AchievementRecord> _achievementListByReferencedId = new();

    private readonly Dictionary<uint, AchievementRewardLocale> _achievementRewardLocales = new();
    private readonly Dictionary<uint, AchievementReward> _achievementRewards = new();
    private readonly Dictionary<uint, uint> _achievementScripts = new();

    // store realm first achievements
    private readonly Dictionary<uint /*achievementId*/, DateTime /*completionTime*/> _allCompletedAchievements = new();

    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly GameObjectManager _gameObjectManager;
    private readonly WorldDatabase _worldDatabase;

    public AchievementGlobalMgr(WorldDatabase worldDatabase, CharacterDatabase characterDatabase, GameObjectManager gameObjectManager, CliDB cliDB)
    {
        _worldDatabase = worldDatabase;
        _characterDatabase = characterDatabase;
        _gameObjectManager = gameObjectManager;
        _cliDB = cliDB;
    }

    public List<AchievementRecord> GetAchievementByReferencedId(uint id)
    {
        return _achievementListByReferencedId.LookupByKey(id);
    }

    public AchievementReward GetAchievementReward(AchievementRecord achievement)
    {
        return _achievementRewards.LookupByKey(achievement.Id);
    }

    public AchievementRewardLocale GetAchievementRewardLocale(AchievementRecord achievement)
    {
        return _achievementRewardLocales.LookupByKey(achievement.Id);
    }

    public uint GetAchievementScriptId(uint achievementId)
    {
        return _achievementScripts.LookupByKey(achievementId);
    }

    public bool IsRealmCompleted(AchievementRecord achievement)
    {
        var time = _allCompletedAchievements.LookupByKey(achievement.Id);

        if (time == default)
            return false;

        if (time == DateTime.MinValue)
            return false;

        if (time == DateTime.MaxValue)
            return true;

        // Allow completing the realm first kill for entire minute after first person did it
        // it may allow more than one group to achieve it (highly unlikely)
        // but apparently this is how blizz handles it as well
        if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstKill))
            return (DateTime.Now - time) > TimeSpan.FromMinutes(1);

        return true;
    }

    //==========================================================
    public void LoadAchievementReferenceList()
    {
        var oldMSTime = Time.MSTime;

        if (_cliDB.AchievementStorage.Empty())
        {
            Log.Logger.Information("Loaded 0 achievement references.");

            return;
        }

        uint count = 0;

        foreach (var achievement in _cliDB.AchievementStorage.Values)
        {
            if (achievement.SharesCriteria == 0)
                continue;

            _achievementListByReferencedId.Add(achievement.SharesCriteria, achievement);
            ++count;
        }

        // Once Bitten, Twice Shy (10 player) - Icecrown Citadel
        var achievement1 = _cliDB.AchievementStorage.LookupByKey(4539u);

        if (achievement1 != null)
            achievement1.InstanceID = 631; // Correct map requirement (currently has Ulduar); 6.0.3 note - it STILL has ulduar requirement

        Log.Logger.Information("Loaded {0} achievement references in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadAchievementScripts()
    {
        var oldMSTime = Time.MSTime;

        _achievementScripts.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT AchievementId, ScriptName FROM achievement_scripts");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 achievement scripts. DB table `achievement_scripts` is empty.");

            return;
        }

        do
        {
            var achievementId = result.Read<uint>(0);
            var scriptName = result.Read<string>(1);

            var achievement = _cliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                Log.Logger.Error($"Table `achievement_scripts` contains non-existing Achievement (ID: {achievementId}), skipped.");

                continue;
            }

            _achievementScripts[achievementId] = _gameObjectManager.GetScriptId(scriptName);
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_achievementScripts.Count} achievement scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadCompletedAchievements()
    {
        var oldMSTime = Time.MSTime;

        // Populate _allCompletedAchievements with all realm first achievement ids to make multithreaded access safer
        // while it will not prevent races, it will prevent crashes that happen because std::unordered_map key was added
        // instead the only potential race will happen on value associated with the key
        foreach (var achievement in _cliDB.AchievementStorage.Values.Where(achievement => achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill)))
            _allCompletedAchievements[achievement.Id] = DateTime.MinValue;

        var result = _characterDatabase.Query("SELECT achievement FROM character_achievement GROUP BY achievement");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 realm first completed achievements. DB table `character_achievement` is empty.");

            return;
        }

        do
        {
            var achievementId = result.Read<uint>(0);
            var achievement = _cliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                // Remove non-existing achievements from all characters
                Log.Logger.Error("Non-existing achievement {0} data has been removed from the table `character_achievement`.", achievementId);

                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEVMENT);
                stmt.AddValue(0, achievementId);
                _characterDatabase.Execute(stmt);
            }
            else if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            {
                _allCompletedAchievements[achievementId] = DateTime.MaxValue;
            }
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} realm first completed achievements in {1} ms.", _allCompletedAchievements.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewardLocales()
    {
        var oldMSTime = Time.MSTime;

        _achievementRewardLocales.Clear(); // need for reload case

        //                                         0   1       2        3
        var result = _worldDatabase.Query("SELECT ID, Locale, Subject, Body FROM achievement_reward_locale");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 achievement reward locale strings.  DB table `achievement_reward_locale` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);

            if (!_achievementRewards.ContainsKey(id))
            {
                Log.Logger.Error($"Table `achievement_reward_locale` (ID: {id}) contains locale strings for a non-existing achievement reward.");

                continue;
            }

            AchievementRewardLocale data = new();
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            GameObjectManager.AddLocaleString(result.Read<string>(2), locale, data.Subject);
            GameObjectManager.AddLocaleString(result.Read<string>(3), locale, data.Body);

            _achievementRewardLocales[id] = data;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} achievement reward locale strings in {1} ms.", _achievementRewardLocales.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewards()
    {
        var oldMSTime = Time.MSTime;

        _achievementRewards.Clear(); // need for reload case

        //                                         0   1       2       3       4       5        6     7
        var result = _worldDatabase.Query("SELECT ID, TitleA, TitleH, ItemID, Sender, Subject, Body, MailTemplateID FROM achievement_reward");

        if (result.IsEmpty())
        {
            Log.Logger.Information(">> Loaded 0 achievement rewards. DB table `achievement_reward` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            var achievement = _cliDB.AchievementStorage.LookupByKey(id);

            if (achievement == null)
            {
                Log.Logger.Error($"Table `achievement_reward` contains a wrong achievement ID ({id}), ignored.");

                continue;
            }

            AchievementReward reward = new()
            {
                TitleId =
                {
                    [0] = result.Read<uint>(1),
                    [1] = result.Read<uint>(2)
                },
                ItemId = result.Read<uint>(3),
                SenderCreatureId = result.Read<uint>(4),
                Subject = result.Read<string>(5),
                Body = result.Read<string>(6),
                MailTemplateId = result.Read<uint>(7)
            };

            // must be title or mail at least
            if (reward.TitleId[0] == 0 && reward.TitleId[1] == 0 && reward.SenderCreatureId == 0)
            {
                Log.Logger.Error($"Table `achievement_reward` (ID: {id}) does not contain title or item reward data. Ignored.");

                continue;
            }

            if (achievement.Faction == AchievementFaction.Any && (reward.TitleId[0] == 0 ^ reward.TitleId[1] == 0))
                Log.Logger.Error($"Table `achievement_reward` (ID: {id}) contains the title (A: {reward.TitleId[0]} H: {reward.TitleId[1]}) for only one team.");

            if (reward.TitleId[0] != 0)
            {
                var titleEntry = _cliDB.CharTitlesStorage.LookupByKey(reward.TitleId[0]);

                if (titleEntry == null)
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[0]}) in `title_A`, set to 0");
                    reward.TitleId[0] = 0;
                }
            }

            if (reward.TitleId[1] != 0)
            {
                var titleEntry = _cliDB.CharTitlesStorage.LookupByKey(reward.TitleId[1]);

                if (titleEntry == null)
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[1]}) in `title_H`, set to 0");
                    reward.TitleId[1] = 0;
                }
            }

            //check mail data before item for report including wrong item case
            if (reward.SenderCreatureId != 0)
            {
                if (_gameObjectManager.GetCreatureTemplate(reward.SenderCreatureId) == null)
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) contains an invalid creature ID {reward.SenderCreatureId} as sender, mail reward skipped.");
                    reward.SenderCreatureId = 0;
                }
            }
            else
            {
                if (reward.ItemId != 0)
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) does not have sender data, but contains an item reward. Item will not be rewarded.");

                if (!reward.Subject.IsEmpty())
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) does not have sender data, but contains a mail subject.");

                if (!reward.Body.IsEmpty())
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) does not have sender data, but contains mail text.");

                if (reward.MailTemplateId != 0)
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) does not have sender data, but has a MailTemplateId.");
            }

            if (reward.MailTemplateId != 0)
            {
                if (!_cliDB.MailTemplateStorage.ContainsKey(reward.MailTemplateId))
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) is using an invalid MailTemplateId ({reward.MailTemplateId}).");
                    reward.MailTemplateId = 0;
                }
                else if (!reward.Subject.IsEmpty() || !reward.Body.IsEmpty())
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) is using MailTemplateId ({reward.MailTemplateId}) and mail subject/text.");
                }
            }

            if (reward.ItemId != 0)
                if (_gameObjectManager.GetItemTemplate(reward.ItemId) == null)
                {
                    Log.Logger.Error($"Table `achievement_reward` (ID: {id}) contains an invalid item id {reward.ItemId}, reward mail will not contain the rewarded item.");
                    reward.ItemId = 0;
                }

            _achievementRewards[id] = reward;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} achievement rewards in {1} ms.", _achievementRewards.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void SetRealmCompleted(AchievementRecord achievement)
    {
        if (IsRealmCompleted(achievement))
            return;

        _allCompletedAchievements[achievement.Id] = DateTime.Now;
    }
}