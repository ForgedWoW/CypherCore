// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Game.Common.Entities.Objects;

namespace Forged.RealmServer.Chat;

public class BroadcastTextBuilder : MessageBuilder
{
	readonly WorldObject _source;
	readonly ChatMsg _msgType;
	readonly uint _textId;
	readonly Gender _gender;
	readonly WorldObject _target;
	readonly uint _achievementId;

	public BroadcastTextBuilder(WorldObject obj, ChatMsg msgtype, uint textId, Gender gender, WorldObject target = null, uint achievementId = 0)
	{
		_source = obj;
		_msgType = msgtype;
		_textId = textId;
		_gender = gender;
		_target = target;
		_achievementId = achievementId;
	}

	public override ChatPacketSender Invoke(Locale locale = Locale.enUS)
	{
		var bct = CliDB.BroadcastTextStorage.LookupByKey(_textId);

		return new ChatPacketSender(_msgType, bct != null ? (Language)bct.LanguageID : Language.Universal, _source, _target, bct != null ? Global.DB2Mgr.GetBroadcastTextValue(bct, locale, _gender) : "", _achievementId, locale);
	}
}