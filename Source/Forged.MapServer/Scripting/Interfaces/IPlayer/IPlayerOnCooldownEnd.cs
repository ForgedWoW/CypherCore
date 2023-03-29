// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Spells;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

public interface IPlayerOnCooldownEnd : IScriptObject, IClassRescriction
{
    void OnCooldownEnd(Player player, SpellInfo spellInfo, uint itemId, uint categoryId);
}