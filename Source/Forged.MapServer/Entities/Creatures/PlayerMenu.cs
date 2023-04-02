// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Networking.Packets.Quest;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Entities.Creatures;

public class PlayerMenu
{
    private readonly IConfiguration _configuration;
    private readonly GossipMenu _gossipMenu;
    private readonly InteractionData _interactionData = new();
    private readonly GameObjectManager _objectManager;
    private readonly QuestMenu _questMenu = new();
    private readonly WorldSession _session;
    private readonly SpellManager _spellManager;
    public PlayerMenu(WorldSession session, GossipMenu gossipMenu, GameObjectManager objectManager, IConfiguration configuration, SpellManager spellManager)
    {
        _session = session;
        _objectManager = objectManager;
        _configuration = configuration;
        _spellManager = spellManager;
        _gossipMenu = gossipMenu;

        if (_session != null)
            _gossipMenu.Locale = _session.SessionDbLocaleIndex;
    }

    public void ClearMenus()
    {
        _gossipMenu.ClearMenu();
        _questMenu.ClearMenu();
    }

    public GossipMenu GetGossipMenu()
    {
        return _gossipMenu;
    }

    public uint GetGossipOptionAction(uint selection)
    {
        return _gossipMenu.GetMenuItemAction(selection);
    }

    public uint GetGossipOptionSender(uint selection)
    {
        return _gossipMenu.GetMenuItemSender(selection);
    }

    public InteractionData GetInteractionData()
    {
        return _interactionData;
    }

    public QuestMenu GetQuestMenu()
    {
        return _questMenu;
    }

    public bool IsGossipOptionCoded(uint selection)
    {
        return _gossipMenu.IsMenuItemCoded(selection);
    }

    public void SendCloseGossip()
    {
        _interactionData.Reset();

        _session.SendPacket(new GossipComplete());
    }

    public void SendGossipMenu(uint titleTextId, ObjectGuid objectGUID)
    {
        _interactionData.Reset();
        _interactionData.SourceGuid = objectGUID;

        GossipMessagePkt packet = new()
        {
            GossipGUID = objectGUID,
            GossipID = _gossipMenu.MenuId
        };

        var addon = _objectManager.GetGossipMenuAddon(packet.GossipID);

        if (addon != null)
            packet.FriendshipFactionID = addon.FriendshipFactionId;

        var text = _objectManager.GetNpcText(titleTextId);

        if (text != null)
            packet.TextID = (int)text.Data.SelectRandomElementByWeight(data => data.Probability).BroadcastTextID;

        foreach (var (_, item) in _gossipMenu.GetMenuItems())
        {
            ClientGossipOptions opt = new()
            {
                GossipOptionID = item.GossipOptionId,
                OptionNPC = item.OptionNpc,
                OptionFlags = (byte)(item.BoxCoded ? 1 : 0), // makes pop up box password
                OptionCost = (int)item.BoxMoney,             // money required to open menu, 2.0.3
                OptionLanguage = item.Language,
                Flags = item.Flags,
                OrderIndex = (int)item.OrderIndex,
                Text = item.OptionText, // text for gossip item
                Confirm = item.BoxText, // accept text (related to money) pop up box, 2.0.3
                Status = GossipOptionStatus.Available,
                SpellID = item.SpellId,
                OverrideIconID = item.OverrideIconId
            };

            packet.GossipOptions.Add(opt);
        }

        for (byte i = 0; i < _questMenu.GetMenuItemCount(); ++i)
        {
            var item = _questMenu.GetItem(i);
            var questID = item.QuestId;
            var quest = _objectManager.GetQuestTemplate(questID);

            if (quest != null)
            {
                ClientGossipText gossipText = new()
                {
                    QuestID = questID,
                    ContentTuningID = quest.ContentTuningId,
                    QuestType = item.QuestIcon,
                    QuestFlags = (uint)quest.Flags,
                    QuestFlagsEx = (uint)quest.FlagsEx,
                    Repeatable = quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly,
                    QuestTitle = quest.LogTitle
                };

                var locale = _session.SessionDbLocaleIndex;

                if (locale != Locale.enUS)
                {
                    var localeData = _objectManager.GetQuestLocale(quest.Id);

                    if (localeData != null)
                        GameObjectManager.GetLocaleString(localeData.LogTitle, locale, ref gossipText.QuestTitle);
                }

                packet.GossipText.Add(gossipText);
            }
        }

        _session.SendPacket(packet);
    }
    public void SendPointOfInterest(uint id)
    {
        var pointOfInterest = _objectManager.GetPointOfInterest(id);

        if (pointOfInterest == null)
        {
            Log.Logger.Error("Request to send non-existing PointOfInterest (Id: {0}), ignored.", id);

            return;
        }

        GossipPOI packet = new()
        {
            Id = pointOfInterest.Id,
            Name = pointOfInterest.Name
        };

        var locale = _session.SessionDbLocaleIndex;

        if (locale != Locale.enUS)
        {
            var localeData = _objectManager.GetPointOfInterestLocale(id);

            if (localeData != null)
                GameObjectManager.GetLocaleString(localeData.Name, locale, ref packet.Name);
        }

        packet.Flags = pointOfInterest.Flags;
        packet.Pos = pointOfInterest.Pos;
        packet.Icon = pointOfInterest.Icon;
        packet.Importance = pointOfInterest.Importance;
        packet.WMOGroupID = pointOfInterest.WmoGroupId;

        _session.SendPacket(packet);
    }

    public void SendQuestGiverOfferReward(Quest.Quest quest, ObjectGuid npcGUID, bool autoLaunched)
    {
        QuestGiverOfferRewardMessage packet = new()
        {
            QuestTitle = quest.LogTitle,
            RewardText = quest.OfferRewardText,
            PortraitGiverText = quest.PortraitGiverText,
            PortraitGiverName = quest.PortraitGiverName,
            PortraitTurnInText = quest.PortraitTurnInText,
            PortraitTurnInName = quest.PortraitTurnInName
        };

        var locale = _session.SessionDbLocaleIndex;

        packet.ConditionalRewardText = quest.ConditionalOfferRewardText.Select(text =>
                                            {
                                                var content = text.Text[(int)Locale.enUS];
                                                GameObjectManager.GetLocaleString(text.Text, locale, ref content);

                                                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
                                            })
                                            .ToList();

        if (locale != Locale.enUS)
        {
            var localeData = _objectManager.GetQuestLocale(quest.Id);

            if (localeData != null)
            {
                GameObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
                GameObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
                GameObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
                GameObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
                GameObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
            }

            var questOfferRewardLocale = _objectManager.GetQuestOfferRewardLocale(quest.Id);

            if (questOfferRewardLocale != null)
                GameObjectManager.GetLocaleString(questOfferRewardLocale.RewardText, locale, ref packet.RewardText);
        }

        QuestGiverOfferReward offer = new();

        quest.BuildQuestRewards(offer.Rewards, _session.Player);
        offer.QuestGiverGUID = npcGUID;

        // Is there a better way? what about game objects?
        var creature = ObjectAccessor.GetCreature(_session.Player, npcGUID);

        if (creature)
        {
            packet.QuestGiverCreatureID = creature.Entry;
            offer.QuestGiverCreatureID = creature.Template.Entry;
        }

        offer.QuestID = quest.Id;
        offer.AutoLaunched = autoLaunched;
        offer.SuggestedPartyMembers = quest.SuggestedPlayers;

        for (uint i = 0; i < SharedConst.QuestEmoteCount && quest.OfferRewardEmote[i] != 0; ++i)
            offer.Emotes.Add(new QuestDescEmote(quest.OfferRewardEmote[i], quest.OfferRewardEmoteDelay[i]));

        offer.QuestFlags[0] = (uint)quest.Flags;
        offer.QuestFlags[1] = (uint)quest.FlagsEx;
        offer.QuestFlags[2] = (uint)quest.FlagsEx2;

        packet.PortraitTurnIn = quest.QuestTurnInPortrait;
        packet.PortraitGiver = quest.QuestGiverPortrait;
        packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
        packet.PortraitGiverModelSceneID = quest.QuestGiverPortraitModelSceneId;
        packet.QuestPackageID = quest.PackageID;

        packet.QuestData = offer;

        _session.SendPacket(packet);
    }

    public void SendQuestGiverQuestDetails(Quest.Quest quest, ObjectGuid npcGUID, bool autoLaunched, bool displayPopup)
    {
        QuestGiverQuestDetails packet = new()
        {
            QuestTitle = quest.LogTitle,
            LogDescription = quest.LogDescription,
            DescriptionText = quest.QuestDescription,
            PortraitGiverText = quest.PortraitGiverText,
            PortraitGiverName = quest.PortraitGiverName,
            PortraitTurnInText = quest.PortraitTurnInText,
            PortraitTurnInName = quest.PortraitTurnInName
        };

        var locale = _session.SessionDbLocaleIndex;

        packet.ConditionalDescriptionText = quest.ConditionalQuestDescription.Select(text =>
                                                 {
                                                     var content = text.Text[(int)Locale.enUS];
                                                     GameObjectManager.GetLocaleString(text.Text, locale, ref content);

                                                     return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
                                                 })
                                                 .ToList();

        if (locale != Locale.enUS)
        {
            var localeData = _objectManager.GetQuestLocale(quest.Id);

            if (localeData != null)
            {
                GameObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
                GameObjectManager.GetLocaleString(localeData.LogDescription, locale, ref packet.LogDescription);
                GameObjectManager.GetLocaleString(localeData.QuestDescription, locale, ref packet.DescriptionText);
                GameObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
                GameObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
                GameObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
                GameObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
            }
        }

        packet.QuestGiverGUID = npcGUID;
        packet.InformUnit = _session.Player.GetPlayerSharingQuest();
        packet.QuestID = quest.Id;
        packet.QuestPackageID = (int)quest.PackageID;
        packet.PortraitGiver = quest.QuestGiverPortrait;
        packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
        packet.PortraitGiverModelSceneID = quest.QuestGiverPortraitModelSceneId;
        packet.PortraitTurnIn = quest.QuestTurnInPortrait;
        packet.QuestSessionBonus = 0; //quest.GetQuestSessionBonus(); // this is only sent while quest session is active
        packet.AutoLaunched = autoLaunched;
        packet.DisplayPopup = displayPopup;
        packet.QuestFlags[0] = (uint)(quest.Flags & (_configuration.GetDefaultValue("Quests.IgnoreAutoAccept", false) ? ~QuestFlags.AutoAccept : ~QuestFlags.None));
        packet.QuestFlags[1] = (uint)quest.FlagsEx;
        packet.QuestFlags[2] = (uint)quest.FlagsEx2;
        packet.SuggestedPartyMembers = quest.SuggestedPlayers;

        // Is there a better way? what about game objects?
        var creature = ObjectAccessor.GetCreature(_session.Player, npcGUID);

        if (creature != null)
            packet.QuestGiverCreatureID = (int)creature.Template.Entry;

        // RewardSpell can teach multiple spells in trigger spell effects. But not all effects must be SPELL_EFFECT_LEARN_SPELL. See example spell 33950
        var spellInfo = _spellManager.GetSpellInfo(quest.RewardSpell);

        if (spellInfo != null)
            foreach (var spellEffectInfo in spellInfo.Effects)
                if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell))
                    packet.LearnSpells.Add(spellEffectInfo.TriggerSpell);

        quest.BuildQuestRewards(packet.Rewards, _session.Player);

        for (var i = 0; i < SharedConst.QuestEmoteCount; ++i)
        {
            var emote = new QuestDescEmote((int)quest.DetailsEmote[i], quest.DetailsEmoteDelay[i]);
            packet.DescEmotes.Add(emote);
        }

        var objs = quest.Objectives;

        for (var i = 0; i < objs.Count; ++i)
        {
            var obj = new QuestObjectiveSimple
            {
                Id = objs[i].Id,
                ObjectID = objs[i].ObjectID,
                Amount = objs[i].Amount,
                Type = (byte)objs[i].Type
            };

            packet.Objectives.Add(obj);
        }

        _session.SendPacket(packet);
    }

    public void SendQuestGiverQuestListMessage(WorldObject questgiver)
    {
        var guid = questgiver.GUID;
        var localeConstant = _session.SessionDbLocaleIndex;

        QuestGiverQuestListMessage questList = new()
        {
            QuestGiverGUID = guid
        };

        var questGreeting = _objectManager.GetQuestGreeting(questgiver.TypeId, questgiver.Entry);

        if (questGreeting != null)
        {
            questList.GreetEmoteDelay = questGreeting.EmoteDelay;
            questList.GreetEmoteType = questGreeting.EmoteType;
            questList.Greeting = questGreeting.Text;

            if (localeConstant != Locale.enUS)
            {
                var questGreetingLocale = _objectManager.GetQuestGreetingLocale(questgiver.TypeId, questgiver.Entry);

                if (questGreetingLocale != null)
                    GameObjectManager.GetLocaleString(questGreetingLocale.Greeting, localeConstant, ref questList.Greeting);
            }
        }

        for (var i = 0; i < _questMenu.GetMenuItemCount(); ++i)
        {
            var questMenuItem = _questMenu.GetItem(i);

            var questID = questMenuItem.QuestId;
            var quest = _objectManager.GetQuestTemplate(questID);

            if (quest != null)
            {
                ClientGossipText text = new()
                {
                    QuestID = questID,
                    ContentTuningID = quest.ContentTuningId,
                    QuestType = questMenuItem.QuestIcon,
                    QuestFlags = (uint)quest.Flags,
                    QuestFlagsEx = (uint)quest.FlagsEx,
                    Repeatable = quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly,
                    QuestTitle = quest.LogTitle
                };

                if (localeConstant != Locale.enUS)
                {
                    var localeData = _objectManager.GetQuestLocale(quest.Id);

                    if (localeData != null)
                        GameObjectManager.GetLocaleString(localeData.LogTitle, localeConstant, ref text.QuestTitle);
                }

                questList.QuestDataText.Add(text);
            }
        }

        _session.SendPacket(questList);
    }

    public void SendQuestGiverRequestItems(Quest.Quest quest, ObjectGuid npcGUID, bool canComplete, bool autoLaunched)
    {
        // We can always call to RequestItems, but this packet only goes out if there are actually
        // items.  Otherwise, we'll skip straight to the OfferReward

        if (!quest.HasQuestObjectiveType(QuestObjectiveType.Item) && canComplete)
        {
            SendQuestGiverOfferReward(quest, npcGUID, true);

            return;
        }

        QuestGiverRequestItems packet = new()
        {
            QuestTitle = quest.LogTitle,
            CompletionText = quest.RequestItemsText
        };

        var locale = _session.SessionDbLocaleIndex;

        packet.ConditionalCompletionText = quest.ConditionalRequestItemsText.Select(text =>
                                                {
                                                    var content = text.Text[(int)Locale.enUS];
                                                    GameObjectManager.GetLocaleString(text.Text, locale, ref content);

                                                    return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
                                                })
                                                .ToList();

        if (locale != Locale.enUS)
        {
            var localeData = _objectManager.GetQuestLocale(quest.Id);

            if (localeData != null)
                GameObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);

            var questRequestItemsLocale = _objectManager.GetQuestRequestItemsLocale(quest.Id);

            if (questRequestItemsLocale != null)
                GameObjectManager.GetLocaleString(questRequestItemsLocale.CompletionText, locale, ref packet.CompletionText);
        }

        packet.QuestGiverGUID = npcGUID;

        // Is there a better way? what about game objects?
        var creature = ObjectAccessor.GetCreature(_session.Player, npcGUID);

        if (creature)
            packet.QuestGiverCreatureID = creature.Template.Entry;

        packet.QuestID = quest.Id;

        if (canComplete)
        {
            packet.CompEmoteDelay = quest.EmoteOnCompleteDelay;
            packet.CompEmoteType = quest.EmoteOnComplete;
        }
        else
        {
            packet.CompEmoteDelay = quest.EmoteOnIncompleteDelay;
            packet.CompEmoteType = quest.EmoteOnIncomplete;
        }

        packet.QuestFlags[0] = (uint)quest.Flags;
        packet.QuestFlags[1] = (uint)quest.FlagsEx;
        packet.QuestFlags[2] = (uint)quest.FlagsEx2;
        packet.SuggestPartyMembers = quest.SuggestedPlayers;

        // incomplete: FD
        // incomplete quest with item objective but item objective is complete DD
        packet.StatusFlags = canComplete ? 0xFF : 0xFD;

        packet.MoneyToGet = 0;

        foreach (var obj in quest.Objectives)
            switch (obj.Type)
            {
                case QuestObjectiveType.Item:
                    packet.Collect.Add(new QuestObjectiveCollect((uint)obj.ObjectID, obj.Amount, (uint)obj.Flags));

                    break;
                case QuestObjectiveType.Currency:
                    packet.Currency.Add(new QuestCurrency((uint)obj.ObjectID, obj.Amount));

                    break;
                case QuestObjectiveType.Money:
                    packet.MoneyToGet += obj.Amount;

                    break;
                default:
                    break;
            }

        packet.AutoLaunched = autoLaunched;

        _session.SendPacket(packet);
    }

    public void SendQuestGiverStatus(QuestGiverStatus questStatus, ObjectGuid npcGUID)
    {
        var packet = new QuestGiverStatusPkt
        {
            QuestGiver =
            {
                Guid = npcGUID,
                Status = questStatus
            }
        };

        _session.SendPacket(packet);
    }
    public void SendQuestQueryResponse(Quest.Quest quest)
    {
        if (_configuration.GetDefaultValue("CacheDataQueries", true))
        {
            _session.SendPacket(quest.response[(int)_session.SessionDbLocaleIndex]);
        }
        else
        {
            var queryPacket = quest.BuildQueryData(_session.SessionDbLocaleIndex, _session.Player);
            _session.SendPacket(queryPacket);
        }
    }
    private bool IsEmpty()
    {
        return _gossipMenu.IsEmpty() && _questMenu.IsEmpty();
    }
}