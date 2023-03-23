// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.A;
using Game.Common.Globals;

namespace Forged.MapServer.Achievements;

public class AchievementGlobalMgr : Singleton<AchievementGlobalMgr>
{
    // store achievements by referenced achievement id to speed up lookup
    readonly MultiMap<uint, AchievementRecord> _achievementListByReferencedId = new();

    // store realm first achievements
    readonly Dictionary<uint /*achievementId*/, DateTime /*completionTime*/> _allCompletedAchievements = new();
    readonly Dictionary<uint, AchievementReward> _achievementRewards = new();
    readonly Dictionary<uint, AchievementRewardLocale> _achievementRewardLocales = new();
    readonly Dictionary<uint, uint> _achievementScripts = new();

    AchievementGlobalMgr() { }

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
            return DateTime.Now - time > TimeSpan.FromMinutes(1);

        return true;
    }

    public void SetRealmCompleted(AchievementRecord achievement)
    {
        if (IsRealmCompleted(achievement))
            return;

        _allCompletedAchievements[achievement.Id] = DateTime.Now;
    }

    //==========================================================
    public void LoadAchievementReferenceList()
    {
        var oldMSTime = Time.MSTime;

        if (CliDB.AchievementStorage.Empty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement references.");

            return;
        }

        uint count = 0;

        foreach (var achievement in CliDB.AchievementStorage.Values)
        {
            if (achievement.SharesCriteria == 0)
                continue;

            _achievementListByReferencedId.Add(achievement.SharesCriteria, achievement);
            ++count;
        }

        // Once Bitten, Twice Shy (10 player) - Icecrown Citadel
        var achievement1 = CliDB.AchievementStorage.LookupByKey(4539);

        if (achievement1 != null)
            achievement1.InstanceID = 631; // Correct map requirement (currently has Ulduar); 6.0.3 note - it STILL has ulduar requirement

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement references in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadAchievementScripts()
    {
        var oldMSTime = Time.MSTime;

        _achievementScripts.Clear(); // need for reload case

        var result = DB.World.Query("SELECT AchievementId, ScriptName FROM achievement_scripts");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement scripts. DB table `achievement_scripts` is empty.");

            return;
        }

        do
        {
            var achievementId = result.Read<uint>(0);
            var scriptName = result.Read<string>(1);

            var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_scripts` contains non-existing Achievement (ID: {achievementId}), skipped.");

                continue;
            }

            _achievementScripts[achievementId] = Global.ObjectMgr.GetScriptId(scriptName);
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, $"Loaded {_achievementScripts.Count} achievement scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadCompletedAchievements()
    {
        var oldMSTime = Time.MSTime;

        // Populate _allCompletedAchievements with all realm first achievement ids to make multithreaded access safer
        // while it will not prevent races, it will prevent crashes that happen because std::unordered_map key was added
        // instead the only potential race will happen on value associated with the key
        foreach (var achievement in CliDB.AchievementStorage.Values)
            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                _allCompletedAchievements[achievement.Id] = DateTime.MinValue;

        var result = DB.Characters.Query("SELECT achievement FROM character_achievement GROUP BY achievement");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 realm first completed achievements. DB table `character_achievement` is empty.");

            return;
        }

        do
        {
            var achievementId = result.Read<uint>(0);
            var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                // Remove non-existing achievements from all characters
                Log.outError(LogFilter.Achievement, "Non-existing achievement {0} data has been removed from the table `character_achievement`.", achievementId);

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEVMENT);
                stmt.AddValue(0, achievementId);
                DB.Characters.Execute(stmt);

                continue;
            }
            else if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            {
                _allCompletedAchievements[achievementId] = DateTime.MaxValue;
            }
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} realm first completed achievements in {1} ms.", _allCompletedAchievements.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewards()
    {
        var oldMSTime = Time.MSTime;

        _achievementRewards.Clear(); // need for reload case

        //                                         0   1       2       3       4       5        6     7
        var result = DB.World.Query("SELECT ID, TitleA, TitleH, ItemID, Sender, Subject, Body, MailTemplateID FROM achievement_reward");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, ">> Loaded 0 achievement rewards. DB table `achievement_reward` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            var achievement = CliDB.AchievementStorage.LookupByKey(id);

            if (achievement == null)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` contains a wrong achievement ID ({id}), ignored.");

                continue;
            }

            AchievementReward reward = new();
            reward.TitleId[0] = result.Read<uint>(1);
            reward.TitleId[1] = result.Read<uint>(2);
            reward.ItemId = result.Read<uint>(3);
            reward.SenderCreatureId = result.Read<uint>(4);
            reward.Subject = result.Read<string>(5);
            reward.Body = result.Read<string>(6);
            reward.MailTemplateId = result.Read<uint>(7);

            // must be title or mail at least
            if (reward.TitleId[0] == 0 && reward.TitleId[1] == 0 && reward.SenderCreatureId == 0)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not contain title or item reward data. Ignored.");

                continue;
            }

            if (achievement.Faction == AchievementFaction.Any && reward.TitleId[0] == 0 ^ reward.TitleId[1] == 0)
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains the title (A: {reward.TitleId[0]} H: {reward.TitleId[1]}) for only one team.");

            if (reward.TitleId[0] != 0)
            {
                var titleEntry = CliDB.CharTitlesStorage.LookupByKey(reward.TitleId[0]);

                if (titleEntry == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[0]}) in `title_A`, set to 0");
                    reward.TitleId[0] = 0;
                }
            }

            if (reward.TitleId[1] != 0)
            {
                var titleEntry = CliDB.CharTitlesStorage.LookupByKey(reward.TitleId[1]);

                if (titleEntry == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[1]}) in `title_H`, set to 0");
                    reward.TitleId[1] = 0;
                }
            }

            //check mail data before item for report including wrong item case
            if (reward.SenderCreatureId != 0)
            {
                if (Global.ObjectMgr.GetCreatureTemplate(reward.SenderCreatureId) == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid creature ID {reward.SenderCreatureId} as sender, mail reward skipped.");
                    reward.SenderCreatureId = 0;
                }
            }
            else
            {
                if (reward.ItemId != 0)
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains an item reward. Item will not be rewarded.");

                if (!reward.Subject.IsEmpty())
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains a mail subject.");

                if (!reward.Body.IsEmpty())
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains mail text.");

                if (reward.MailTemplateId != 0)
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but has a MailTemplateId.");
            }

            if (reward.MailTemplateId != 0)
            {
                if (!CliDB.MailTemplateStorage.ContainsKey(reward.MailTemplateId))
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using an invalid MailTemplateId ({reward.MailTemplateId}).");
                    reward.MailTemplateId = 0;
                }
                else if (!reward.Subject.IsEmpty() || !reward.Body.IsEmpty())
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using MailTemplateId ({reward.MailTemplateId}) and mail subject/text.");
                }
            }

            if (reward.ItemId != 0)
                if (Global.ObjectMgr.GetItemTemplate(reward.ItemId) == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid item id {reward.ItemId}, reward mail will not contain the rewarded item.");
                    reward.ItemId = 0;
                }

            _achievementRewards[id] = reward;
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement rewards in {1} ms.", _achievementRewards.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewardLocales()
    {
        var oldMSTime = Time.MSTime;

        _achievementRewardLocales.Clear(); // need for reload case

        //                                         0   1       2        3
        var result = DB.World.Query("SELECT ID, Locale, Subject, Body FROM achievement_reward_locale");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement reward locale strings.  DB table `achievement_reward_locale` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);

            if (!_achievementRewards.ContainsKey(id))
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward_locale` (ID: {id}) contains locale strings for a non-existing achievement reward.");

                continue;
            }

            AchievementRewardLocale data = new();
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            ObjectManager.AddLocaleString(result.Read<string>(2), locale, data.Subject);
            ObjectManager.AddLocaleString(result.Read<string>(3), locale, data.Body);

            _achievementRewardLocales[id] = data;
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement reward locale strings in {1} ms.", _achievementRewardLocales.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public uint GetAchievementScriptId(uint achievementId)
    {
        return _achievementScripts.LookupByKey(achievementId);
    }
}