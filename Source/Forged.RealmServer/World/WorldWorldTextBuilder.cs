// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Forged.RealmServer.Chat;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Players;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Chat;

namespace Forged.RealmServer;

public class WorldWorldTextBuilder : MessageBuilder
{
	readonly uint _iTextId;
	readonly object[] _iArgs;

	public WorldWorldTextBuilder(uint textId, params object[] args)
	{
		_iTextId = textId;
		_iArgs = args;
	}

	public override MultiplePacketSender Invoke(Locale locale)
	{
		var text = _gameObjectManager.GetCypherString(_iTextId, locale);

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