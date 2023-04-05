// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Chat;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class ChatPacketSender : IDoWork<Player>
{
    public ChatPkt TranslatedPacket;

    // caches
    public ChatPkt UntranslatedPacket;
    private readonly uint _achievementId;
    private readonly Language _language;
    private readonly Locale _locale;
    private readonly WorldObject _receiver;
    private readonly WorldObject _sender;
    private readonly string _text;
    private readonly ChatMsg _type;
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
        if (_language is Language.Universal or Language.Addon or Language.AddonLogged || player.CanUnderstandLanguage(_language))
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