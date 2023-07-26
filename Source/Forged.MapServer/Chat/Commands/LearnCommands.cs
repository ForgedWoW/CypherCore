// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Spells;
using Framework.Constants;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("learn")]
internal class LearnCommands
{
    [Command("", CypherStrings.CommandLearnHelp, RBACPermissions.CommandLearn)]
    private static bool HandleLearnCommand(CommandHandler handler, uint spellId, string allRanksStr)
    {
        var targetPlayer = handler.SelectedPlayerOrSelf;

        if (targetPlayer == null)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        if (!handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellId, handler.Session.Player))
        {
            handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellId);

            return false;
        }

        var allRanks = !allRanksStr.IsEmpty() && allRanksStr.Equals("all", StringComparison.OrdinalIgnoreCase);

        if (!allRanks && targetPlayer.HasSpell(spellId))
        {
            if (targetPlayer == handler.Player)
                handler.SendSysMessage(CypherStrings.YouKnownSpell);
            else
                handler.SendSysMessage(CypherStrings.TargetKnownSpell, handler.GetNameLink(targetPlayer));

            return false;
        }

        targetPlayer.LearnSpell(spellId, false);

        if (allRanks)
            while ((spellId = handler.ClassFactory.Resolve<SpellManager>().GetNextSpellInChain(spellId)) != 0)
                targetPlayer.LearnSpell(spellId, false);

        return true;
    }

    [CommandNonGroup("unlearn", CypherStrings.CommandUnlearnHelp, RBACPermissions.CommandUnlearn)]
    private static bool HandleUnLearnCommand(CommandHandler handler, uint spellId, string allRanksStr)
    {
        var target = handler.SelectedPlayer;

        if (target == null)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        var allRanks = !allRanksStr.IsEmpty() && allRanksStr.Equals("all", StringComparison.OrdinalIgnoreCase);

        spellId = allRanks switch
        {
            true => handler.ClassFactory.Resolve<SpellManager>().GetFirstSpellInChain(spellId),
            _    => spellId
        };

        if (target.HasSpell(spellId))
            target.RemoveSpell(spellId, false, !allRanks);
        else
            handler.SendSysMessage(CypherStrings.ForgetSpell);

        return true;
    }

    [CommandGroup("all")]
    private class LearnAllCommands
    {
        [Command("crafts", CypherStrings.CommandLearnAllCraftsHelp, RBACPermissions.CommandLearnAllCrafts)]
        private static bool HandleLearnAllCraftsCommand(CommandHandler handler, PlayerIdentifier player)
        {
            player = player switch
            {
                null => PlayerIdentifier.FromTargetOrSelf(handler),
                _    => player
            };

            if (player == null || !player.IsConnected())
                return false;

            var target = player.GetConnectedPlayer();

            foreach (var (_, skillInfo) in handler.CliDB.SkillLineStorage)
                if (skillInfo.CategoryID is SkillCategory.Profession or SkillCategory.Secondary && skillInfo.CanLink != 0) // only prof. with recipes have
                    HandleLearnSkillRecipesHelper(target, skillInfo.Id, handler);

            handler.SendSysMessage(CypherStrings.CommandLearnAllCraft);

            return true;
        }

        [Command("default", CypherStrings.CommandLearnAllDefaultHelp, RBACPermissions.CommandLearnAllDefault)]
        private static bool HandleLearnAllDefaultCommand(CommandHandler handler, PlayerIdentifier player)
        {
            player = player switch
            {
                null => PlayerIdentifier.FromTargetOrSelf(handler),
                _    => player
            };

            if (player == null || !player.IsConnected())
                return false;

            var target = player.GetConnectedPlayer();
            target.LearnDefaultSkills();
            target.LearnCustomSpells();
            target.LearnQuestRewardedSpells();

            handler.SendSysMessage(CypherStrings.CommandLearnAllDefaultAndQuest, handler.GetNameLink(target));

            return true;
        }

        [Command("blizzard", CypherStrings.CommandLearnAllBlizzardHelp, RBACPermissions.CommandLearnAllGm)]
        private static bool HandleLearnAllGMCommand(CommandHandler handler)
        {
            foreach (var skillSpell in handler.ClassFactory.Resolve<SpellManager>().GetSkillLineAbilityMapBounds((uint)SkillType.Internal))
            {
                var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(skillSpell.Spell);

                if (spellInfo == null || !handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellInfo, handler.Session.Player, false))
                    continue;

                handler.Session.Player.LearnSpell(skillSpell.Spell, false);
            }

            handler.SendSysMessage(CypherStrings.LearningGmSkills);

            return true;
        }

        [Command("languages", CypherStrings.CommandLearnAllLanguagesHelp, RBACPermissions.CommandLearnAllLang)]
        private static bool HandleLearnAllLangCommand(CommandHandler handler)
        {
            handler.ClassFactory.Resolve<LanguageManager>()
                   .ForEachLanguage((_, languageDesc) =>
                   {
                       if (languageDesc.SpellId != 0)
                           handler.Session.Player.LearnSpell(languageDesc.SpellId, false);

                       return true;
                   });

            handler.SendSysMessage(CypherStrings.CommandLearnAllLang);

            return true;
        }

        [Command("pettalents", CypherStrings.CommandLearnAllPettalentHelp, RBACPermissions.CommandLearnMyPetTalents)]
        private static bool HandleLearnAllPetTalentsCommand(CommandHandler handler)
        {
            return handler != null;
        }

        [Command("recipes", CypherStrings.CommandLearnAllRecipesHelp, RBACPermissions.CommandLearnAllRecipes)]
        private static bool HandleLearnAllRecipesCommand(CommandHandler handler, Tail namePart)
        {
            //  Learns all recipes of specified profession and sets skill to max
            //  Example: .learn all_recipes enchanting

            var target = handler.SelectedPlayer;

            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            if (namePart.IsEmpty())
                return false;

            var name = "";
            uint skillId = 0;

            foreach (var (_, skillInfo) in handler.CliDB.SkillLineStorage)
            {
                if ((skillInfo.CategoryID != SkillCategory.Profession &&
                     skillInfo.CategoryID != SkillCategory.Secondary) ||
                    skillInfo.CanLink == 0) // only prof with recipes have set
                    continue;

                var locale = handler.SessionDbcLocale;
                name = skillInfo.DisplayName[locale];

                if (string.IsNullOrEmpty(name))
                    continue;

                if (!name.Like(namePart))
                {
                    locale = 0;

                    for (; locale < Locale.Total; ++locale)
                    {
                        name = skillInfo.DisplayName[locale];

                        if (name.IsEmpty())
                            continue;

                        if (name.Like(namePart))
                            break;
                    }
                }

                if (locale < Locale.Total)
                {
                    skillId = skillInfo.Id;

                    break;
                }
            }

            if (!(name.IsEmpty() && skillId != 0))
                return false;

            HandleLearnSkillRecipesHelper(target, skillId, handler);

            var maxLevel = target.GetPureMaxSkillValue((SkillType)skillId);
            target.SetSkill(skillId, target.GetSkillStep((SkillType)skillId), maxLevel, maxLevel);
            handler.SendSysMessage(CypherStrings.CommandLearnAllRecipes, name);

            return true;
        }

        [Command("talents", CypherStrings.CommandLearnAllTalentsHelp, RBACPermissions.CommandLearnAllTalents)]
        private static bool HandleLearnAllTalentsCommand(CommandHandler handler)
        {
            var player = handler.Session.Player;
            var playerClass = (uint)player.Class;

            foreach (var (_, talentInfo) in handler.CliDB.TalentStorage)
            {
                if (playerClass != talentInfo.ClassID)
                    continue;

                if (talentInfo.SpecID != 0 && player.GetPrimarySpecialization() != talentInfo.SpecID)
                    continue;

                var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(talentInfo.SpellID);

                if (spellInfo == null || !handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellInfo, handler.Session.Player, false))
                    continue;

                // learn highest rank of talent and learn all non-talent spell ranks (recursive by tree)
                player.AddTalent(talentInfo, player.GetActiveTalentGroup(), true);
                player.LearnSpell(talentInfo.SpellID, false);
            }

            player.SendTalentsInfoData();

            handler.SendSysMessage(CypherStrings.CommandLearnClassTalents);

            return true;
        }

        [Command("debug", CypherStrings.CommandLearnAllDebugHelp, RBACPermissions.CommandLearn)]
        private static bool HandleLearnDebugSpellsCommand(CommandHandler handler)
        {
            var player = handler.Player;
            player.LearnSpell(63364, false); /* 63364 - Saronite Barrier (reduces damage taken by 99%) */
            player.LearnSpell(1908, false);  /*  1908 - Uber Heal Over Time (heals target to full constantly) */
            player.LearnSpell(27680, false); /* 27680 - Berserk (+500% damage, +150% speed, 10m duration) */
            player.LearnSpell(62555, false); /* 62555 - Berserk (+500% damage, +150% melee haste, 10m duration) */
            player.LearnSpell(64238, false); /* 64238 - Berserk (+900% damage, +150% melee haste, 30m duration) */
            player.LearnSpell(72525, false); /* 72525 - Berserk (+240% damage, +160% haste, infinite duration) */
            player.LearnSpell(66776, false); /* 66776 - Rage (+300% damage, -95% damage taken, +100% speed, infinite duration) */

            return true;
        }

        private static void HandleLearnSkillRecipesHelper(Player player, uint skillId, CommandHandler handler)
        {
            var classmask = player.ClassMask;

            var skillLineAbilities = handler.ClassFactory.Resolve<DB2Manager>().GetSkillLineAbilitiesBySkill(skillId);

            if (skillLineAbilities == null)
                return;

            foreach (var skillLine in skillLineAbilities)
            {
                // not high rank
                if (skillLine.SupercedesSpell != 0)
                    continue;

                // skip racial skills
                if (skillLine.RaceMask != 0)
                    continue;

                // skip wrong class skills
                if (skillLine.ClassMask != 0 && (skillLine.ClassMask & classmask) == 0)
                    continue;

                var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(skillLine.Spell);

                if (spellInfo == null || !handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellInfo, player, false))
                    continue;

                player.LearnSpell(skillLine.Spell, false);
            }
        }
    }

    [CommandGroup("my")]
    private class LearnAllMyCommands
    {
        [Command("quests", CypherStrings.CommandLearnMyQuestsHelp, RBACPermissions.CommandLearnAllMySpells)]
        private static bool HandleLearnMyQuestsCommand(CommandHandler handler)
        {
            var player = handler.Player;

            foreach (var (_, quest) in handler.ObjectManager.QuestTemplates)
                if (quest.AllowableClasses != 0 && player.SatisfyQuestClass(quest, false))
                    player.LearnQuestRewardedSpells(quest);

            return true;
        }

        [Command("trainer", CypherStrings.CommandLearnMyTrainerHelp, RBACPermissions.CommandLearnAllMySpells)]
        private static bool HandleLearnMySpellsCommand(CommandHandler handler)
        {
            if (!handler.CliDB.ChrClassesStorage.TryGetValue((uint)handler.Player.Class, out var classEntry))
                return true;

            uint family = classEntry.SpellClassSet;

            foreach (var (_, entry) in handler.CliDB.SkillLineAbilityStorage)
            {
                var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(entry.Spell);

                if (spellInfo == null)
                    continue;

                // skip server-side/triggered spells
                if (spellInfo.SpellLevel == 0)
                    continue;

                // skip wrong class/race skills
                if (!handler.Session.Player.IsSpellFitByClassAndRace(spellInfo.Id))
                    continue;

                // skip other spell families
                if ((uint)spellInfo.SpellFamilyName != family)
                    continue;

                // skip broken spells
                if (!handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellInfo, handler.Session.Player, false))
                    continue;

                handler.Session.Player.LearnSpell(spellInfo.Id, false);
            }

            handler.SendSysMessage(CypherStrings.CommandLearnClassSpells);

            return true;
        }
    }
}