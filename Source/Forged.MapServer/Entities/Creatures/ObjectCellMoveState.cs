// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Creatures;

public enum ObjectCellMoveState
{
    None,    // not in move list
    Active,  // in move list
    Inactive // in move list but should not move
}