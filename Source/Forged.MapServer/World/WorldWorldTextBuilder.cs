// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Text;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.World;

public class WorldWorldTextBuilder : MessageBuilder
{
    private readonly object[] _iArgs;
    private readonly GameObjectManager _objectManager;
    private readonly uint _iTextId;
    public WorldWorldTextBuilder(GameObjectManager objectManager, uint textId, params object[] args)
    {
        _objectManager = objectManager;
        _iTextId = textId;
        _iArgs = args;
    }

    public override MultiplePacketSender Invoke(Locale locale = Locale.enUS)
    {
        var text = _objectManager.GetCypherString(_iTextId, locale);

        if (_iArgs != null)
            text = string.Format(text, _iArgs);

        MultiplePacketSender sender = new();

        var lines = new StringArray(text, "\n");

        for (var i = 0; i < lines.Length; ++i)
        {
            ChatPkt messageChat = new();
            messageChat.Initialize(ChatMsg.System, Language.Universal, null, null, lines[i]);
            messageChat.Write();
            sender.Packets.Add(messageChat);
        }

        return sender;
    }

    public class MultiplePacketSender : IDoWork<Player>
    {
        public List<ServerPacket> Packets = new();

        public void Invoke(Player receiver)
        {
            foreach (var packet in Packets)
                receiver.SendPacket(packet);
        }
    }
}