// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Scripting.Interfaces;
using Game.Scripting.Interfaces.IPlayer;

namespace Game.Common.Scripting.Interfaces.IPlayer;

public interface IPlayerOnChatWhisper : IScriptObject
{
	void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver);
}
