// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a Duel ends
public interface IPlayerOnDuelEnd : IScriptObject
{
    void OnDuelEnd(Player winner, Player loser, DuelCompleteType type);
}