﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Both of the below are called on Emote opcodes.
public interface IPlayerOnClearEmote : IScriptObject
{
    void OnClearEmote(Player player);
}