// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

public interface IPlayerOnModifyPower : IScriptObject, IClassRescriction
{
	void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen);
}