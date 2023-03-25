// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Misc;

public class PlayerMenu
{
	readonly GossipMenu _gossipMenu = new();
	readonly QuestMenu _questMenu = new();
	readonly WorldSession _session;
	readonly InteractionData _interactionData = new();

	public PlayerMenu(WorldSession session)
	{
		_session = session;

		if (_session != null)
			_gossipMenu.SetLocale(_session.SessionDbLocaleIndex);
	}

	public void ClearMenus()
	{
		_gossipMenu.ClearMenu();
		_questMenu.ClearMenu();
	}

	public void SendGossipMenu(uint titleTextId, ObjectGuid objectGUID)
	{
		_interactionData.Reset();
		_interactionData.SourceGuid = objectGUID;

		GossipMessagePkt packet = new();
		packet.GossipGUID = objectGUID;
		packet.GossipID = _gossipMenu.GetMenuId();

		var addon = Global.ObjectMgr.GetGossipMenuAddon(packet.GossipID);

		if (addon != null)
			packet.FriendshipFactionID = addon.FriendshipFactionId;

		var text = Global.ObjectMgr.GetNpcText(titleTextId);

		if (text != null)
			packet.TextID = (int)text.Data.SelectRandomElementByWeight(data => data.Probability).BroadcastTextID;

		foreach (var (index, item) in _gossipMenu.GetMenuItems())
		{
			ClientGossipOptions opt = new();
			opt.GossipOptionID = item.GossipOptionId;
			opt.OptionNPC = item.OptionNpc;
			opt.OptionFlags = (byte)(item.BoxCoded ? 1 : 0); // makes pop up box password
			opt.OptionCost = (int)item.BoxMoney;             // money required to open menu, 2.0.3
			opt.OptionLanguage = item.Language;
			opt.Flags = item.Flags;
			opt.OrderIndex = (int)item.OrderIndex;
			opt.Text = item.OptionText; // text for gossip item
			opt.Confirm = item.BoxText; // accept text (related to money) pop up box, 2.0.3
			opt.Status = GossipOptionStatus.Available;
			opt.SpellID = item.SpellId;
			opt.OverrideIconID = item.OverrideIconId;
			packet.GossipOptions.Add(opt);
		}

		for (byte i = 0; i < _questMenu.GetMenuItemCount(); ++i)
		{
			var item = _questMenu.GetItem(i);
			var questID = item.QuestId;
			var quest = Global.ObjectMgr.GetQuestTemplate(questID);

			if (quest != null)
			{
				ClientGossipText gossipText = new();
				gossipText.QuestID = questID;
				gossipText.ContentTuningID = quest.ContentTuningId;
				gossipText.QuestType = item.QuestIcon;
				gossipText.QuestFlags = (uint)quest.Flags;
				gossipText.QuestFlagsEx = (uint)quest.FlagsEx;
				gossipText.Repeatable = quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly;

				gossipText.QuestTitle = quest.LogTitle;
				var locale = _session.SessionDbLocaleIndex;

				if (locale != Locale.enUS)
				{
					var localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);

					if (localeData != null)
						ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref gossipText.QuestTitle);
				}

				packet.GossipText.Add(gossipText);
			}
		}

		_session.SendPacket(packet);
	}

	public void SendCloseGossip()
	{
		_interactionData.Reset();

		_session.SendPacket(new GossipComplete());
	}

	public void SendPointOfInterest(uint id)
	{
		var pointOfInterest = Global.ObjectMgr.GetPointOfInterest(id);

		if (pointOfInterest == null)
		{
			Log.Logger.Error("Request to send non-existing PointOfInterest (Id: {0}), ignored.", id);

			return;
		}

		GossipPOI packet = new();
		packet.Id = pointOfInterest.Id;
		packet.Name = pointOfInterest.Name;

		var locale = _session.SessionDbLocaleIndex;

		if (locale != Locale.enUS)
		{
			var localeData = Global.ObjectMgr.GetPointOfInterestLocale(id);

			if (localeData != null)
				ObjectManager.GetLocaleString(localeData.Name, locale, ref packet.Name);
		}

		packet.Flags = pointOfInterest.Flags;
		packet.Pos = pointOfInterest.Pos;
		packet.Icon = pointOfInterest.Icon;
		packet.Importance = pointOfInterest.Importance;
		packet.WMOGroupID = pointOfInterest.WmoGroupId;

		_session.SendPacket(packet);
	}

	public void SendQuestGiverQuestListMessage(WorldObject questgiver)
	{
		var guid = questgiver.GUID;
		var localeConstant = _session.SessionDbLocaleIndex;

		QuestGiverQuestListMessage questList = new();
		questList.QuestGiverGUID = guid;

		var questGreeting = Global.ObjectMgr.GetQuestGreeting(questgiver.TypeId, questgiver.Entry);

		if (questGreeting != null)
		{
			questList.GreetEmoteDelay = questGreeting.EmoteDelay;
			questList.GreetEmoteType = questGreeting.EmoteType;
			questList.Greeting = questGreeting.Text;

			if (localeConstant != Locale.enUS)
			{
				var questGreetingLocale = Global.ObjectMgr.GetQuestGreetingLocale(questgiver.TypeId, questgiver.Entry);

				if (questGreetingLocale != null)
					ObjectManager.GetLocaleString(questGreetingLocale.Greeting, localeConstant, ref questList.Greeting);
			}
		}

		for (var i = 0; i < _questMenu.GetMenuItemCount(); ++i)
		{
			var questMenuItem = _questMenu.GetItem(i);

			var questID = questMenuItem.QuestId;
			var quest = Global.ObjectMgr.GetQuestTemplate(questID);

			if (quest != null)
			{
				ClientGossipText text = new();
				text.QuestID = questID;
				text.ContentTuningID = quest.ContentTuningId;
				text.QuestType = questMenuItem.QuestIcon;
				text.QuestFlags = (uint)quest.Flags;
				text.QuestFlagsEx = (uint)quest.FlagsEx;
				text.Repeatable = quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly;
				text.QuestTitle = quest.LogTitle;

				if (localeConstant != Locale.enUS)
				{
					var localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);

					if (localeData != null)
						ObjectManager.GetLocaleString(localeData.LogTitle, localeConstant, ref text.QuestTitle);
				}

				questList.QuestDataText.Add(text);
			}
		}

		_session.SendPacket(questList);
	}

	public void SendQuestGiverStatus(QuestGiverStatus questStatus, ObjectGuid npcGUID)
	{
		var packet = new QuestGiverStatusPkt();
		packet.QuestGiver.Guid = npcGUID;
		packet.QuestGiver.Status = questStatus;

		_session.SendPacket(packet);
	}

	public void SendQuestGiverQuestDetails(Quest quest, ObjectGuid npcGUID, bool autoLaunched, bool displayPopup)
	{
		QuestGiverQuestDetails packet = new();

		packet.QuestTitle = quest.LogTitle;
		packet.LogDescription = quest.LogDescription;
		packet.DescriptionText = quest.QuestDescription;
		packet.PortraitGiverText = quest.PortraitGiverText;
		packet.PortraitGiverName = quest.PortraitGiverName;
		packet.PortraitTurnInText = quest.PortraitTurnInText;
		packet.PortraitTurnInName = quest.PortraitTurnInName;

		var locale = _session.SessionDbLocaleIndex;

		packet.ConditionalDescriptionText = quest.ConditionalQuestDescription.Select(text =>
												{
													var content = text.Text[(int)Locale.enUS];
													ObjectManager.GetLocaleString(text.Text, locale, ref content);

													return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
												})
												.ToList();

		if (locale != Locale.enUS)
		{
			var localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);

			if (localeData != null)
			{
				ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
				ObjectManager.GetLocaleString(localeData.LogDescription, locale, ref packet.LogDescription);
				ObjectManager.GetLocaleString(localeData.QuestDescription, locale, ref packet.DescriptionText);
				ObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
				ObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
				ObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
				ObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
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
		packet.QuestFlags[0] = (uint)(quest.Flags & (WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoAccept) ? ~QuestFlags.AutoAccept : ~QuestFlags.None));
		packet.QuestFlags[1] = (uint)quest.FlagsEx;
		packet.QuestFlags[2] = (uint)quest.FlagsEx2;
		packet.SuggestedPartyMembers = quest.SuggestedPlayers;

		// Is there a better way? what about game objects?
		var creature = ObjectAccessor.GetCreature(_session.Player, npcGUID);

		if (creature != null)
			packet.QuestGiverCreatureID = (int)creature.Template.Entry;

		// RewardSpell can teach multiple spells in trigger spell effects. But not all effects must be SPELL_EFFECT_LEARN_SPELL. See example spell 33950
		var spellInfo = Global.SpellMgr.GetSpellInfo(quest.RewardSpell, Difficulty.None);

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
			var obj = new QuestObjectiveSimple();
			obj.Id = objs[i].Id;
			obj.ObjectID = objs[i].ObjectID;
			obj.Amount = objs[i].Amount;
			obj.Type = (byte)objs[i].Type;
			packet.Objectives.Add(obj);
		}

		_session.SendPacket(packet);
	}

	public void SendQuestQueryResponse(Quest quest)
	{
		if (WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
		{
			_session.SendPacket(quest.response[(int)_session.SessionDbLocaleIndex]);
		}
		else
		{
			var queryPacket = quest.BuildQueryData(_session.SessionDbLocaleIndex, _session.Player);
			_session.SendPacket(queryPacket);
		}
	}

	public void SendQuestGiverOfferReward(Quest quest, ObjectGuid npcGUID, bool autoLaunched)
	{
		QuestGiverOfferRewardMessage packet = new();

		packet.QuestTitle = quest.LogTitle;
		packet.RewardText = quest.OfferRewardText;
		packet.PortraitGiverText = quest.PortraitGiverText;
		packet.PortraitGiverName = quest.PortraitGiverName;
		packet.PortraitTurnInText = quest.PortraitTurnInText;
		packet.PortraitTurnInName = quest.PortraitTurnInName;

		var locale = _session.SessionDbLocaleIndex;

		packet.ConditionalRewardText = quest.ConditionalOfferRewardText.Select(text =>
											{
												var content = text.Text[(int)Locale.enUS];
												ObjectManager.GetLocaleString(text.Text, locale, ref content);

												return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
											})
											.ToList();

		if (locale != Locale.enUS)
		{
			var localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);

			if (localeData != null)
			{
				ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
				ObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
				ObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
				ObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
				ObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
			}

			var questOfferRewardLocale = Global.ObjectMgr.GetQuestOfferRewardLocale(quest.Id);

			if (questOfferRewardLocale != null)
				ObjectManager.GetLocaleString(questOfferRewardLocale.RewardText, locale, ref packet.RewardText);
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

	public void SendQuestGiverRequestItems(Quest quest, ObjectGuid npcGUID, bool canComplete, bool autoLaunched)
	{
		// We can always call to RequestItems, but this packet only goes out if there are actually
		// items.  Otherwise, we'll skip straight to the OfferReward

		if (!quest.HasQuestObjectiveType(QuestObjectiveType.Item) && canComplete)
		{
			SendQuestGiverOfferReward(quest, npcGUID, true);

			return;
		}

		QuestGiverRequestItems packet = new();

		packet.QuestTitle = quest.LogTitle;
		packet.CompletionText = quest.RequestItemsText;

		var locale = _session.SessionDbLocaleIndex;

		packet.ConditionalCompletionText = quest.ConditionalRequestItemsText.Select(text =>
												{
													var content = text.Text[(int)Locale.enUS];
													ObjectManager.GetLocaleString(text.Text, locale, ref content);

													return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
												})
												.ToList();

		if (locale != Locale.enUS)
		{
			var localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);

			if (localeData != null)
				ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);

			var questRequestItemsLocale = Global.ObjectMgr.GetQuestRequestItemsLocale(quest.Id);

			if (questRequestItemsLocale != null)
				ObjectManager.GetLocaleString(questRequestItemsLocale.CompletionText, locale, ref packet.CompletionText);
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

	public GossipMenu GetGossipMenu()
	{
		return _gossipMenu;
	}

	public QuestMenu GetQuestMenu()
	{
		return _questMenu;
	}

	public InteractionData GetInteractionData()
	{
		return _interactionData;
	}

	public uint GetGossipOptionSender(uint selection)
	{
		return _gossipMenu.GetMenuItemSender(selection);
	}

	public uint GetGossipOptionAction(uint selection)
	{
		return _gossipMenu.GetMenuItemAction(selection);
	}

	public bool IsGossipOptionCoded(uint selection)
	{
		return _gossipMenu.IsMenuItemCoded(selection);
	}

	bool IsEmpty()
	{
		return _gossipMenu.IsEmpty() && _questMenu.IsEmpty();
	}
}