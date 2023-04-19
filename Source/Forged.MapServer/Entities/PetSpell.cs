// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities;

public class PetSpell
{
    public ActiveStates Active { get; set; }
    public PetSpellState State { get; set; }
    public PetSpellType Type { get; set; }
}