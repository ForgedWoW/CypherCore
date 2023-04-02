// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class BroadcastTextBuilder : MessageBuilder
{
    private readonly uint _achievementId;
    private readonly Gender _gender;
    private readonly ChatMsg _msgType;
    private readonly WorldObject _source;
    private readonly WorldObject _target;
    private readonly uint _textId;
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