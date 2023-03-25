// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player takes damage
public interface IPlayerOnTakeDamage : IScriptObject, IClassRescriction
{
	void OnPlayerTakeDamage(Player player, double amount, SpellSchoolMask schoolMask);
}