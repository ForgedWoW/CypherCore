// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class CustomChatTextBuilder : MessageBuilder
{
    private readonly Language _language;
    private readonly ChatMsg _msgType;
    private readonly WorldObject _source;
    private readonly WorldObject _target;
    private readonly string _text;
    public CustomChatTextBuilder(WorldObject obj, ChatMsg msgType, string text, Language language = Language.Universal, WorldObject target = null)
    {
        _source = obj;
        _msgType = msgType;
        _text = text;
        _language = language;
        _target = target;
    }

    public override ChatPacketSender Invoke(Locale locale)
    {
        return new ChatPacketSender(_msgType, _language, _source, _target, _text, 0, locale);
    }
}