// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat;

public class ChatPacketSender : IDoWork<Player>
{
	// caches
	public ChatPkt UntranslatedPacket;
	public ChatPkt TranslatedPacket;
	readonly ChatMsg _type;
	readonly Language _language;
	readonly WorldObject _sender;
	readonly WorldObject _receiver;
	readonly string _text;
	readonly uint _achievementId;
	readonly Locale _locale;

	public ChatPacketSender(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, Locale locale = Locale.enUS)
	{
		_type = chatType;
		_language = language;
		_sender = sender;
		_receiver = receiver;
		_text = message;
		_achievementId = achievementId;
		_locale = locale;

		UntranslatedPacket = new ChatPkt();
		UntranslatedPacket.Initialize(_type, _language, _sender, _receiver, _text, _achievementId, "", _locale);
		UntranslatedPacket.Write();
	}

	public void Invoke(Player player)
	{
		if (_language == Language.Universal || _language == Language.Addon || _language == Language.AddonLogged || player.CanUnderstandLanguage(_language))
		{
			player.SendPacket(UntranslatedPacket);

			return;
		}

		if (TranslatedPacket == null)
		{
			TranslatedPacket = new ChatPkt();
			TranslatedPacket.Initialize(_type, _language, _sender, _receiver, Global.LanguageMgr.Translate(_text, (uint)_language, player.Session.SessionDbcLocale), _achievementId, "", _locale);
			TranslatedPacket.Write();
		}

		player.SendPacket(TranslatedPacket);
	}
}