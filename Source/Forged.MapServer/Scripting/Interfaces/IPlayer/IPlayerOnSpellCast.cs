// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Spells;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

/// <summary>
///  Called when the player casts a spell
/// </summary>
public interface IPlayerOnSpellCast : IScriptObject, IClassRescriction
{
	void OnSpellCast(Player player, Spell spell, bool skipCheck);
}