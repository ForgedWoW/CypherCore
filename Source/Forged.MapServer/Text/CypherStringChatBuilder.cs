﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Text;

internal class CypherStringChatBuilder : MessageBuilder
{
    private readonly WorldObject _source;
    private readonly ChatMsg _msgType;
    private readonly CypherStrings _textId;
    private readonly WorldObject _target;
    private readonly object[] _args;

    public CypherStringChatBuilder(WorldObject obj, ChatMsg msgType, CypherStrings textId, WorldObject target = null, object[] args = null)
    {
        _source = obj;
        _msgType = msgType;
        _textId = textId;
        _target = target;
        _args = args;
    }

    public override ChatPacketSender Invoke(Locale locale)
    {
        var text = Global.ObjectMgr.GetCypherString(_textId, locale);

        if (_args != null)
            return new ChatPacketSender(_msgType, Language.Universal, _source, _target, string.Format(text, _args), 0, locale);
        else
            return new ChatPacketSender(_msgType, Language.Universal, _source, _target, text, 0, locale);
    }
}