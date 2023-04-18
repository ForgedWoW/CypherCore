// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class PlayerSpell
{
    public bool Active { get; set; }
    public bool Dependent { get; set; }
    public bool Disabled { get; set; }
    public bool Favorite { get; set; }
    public PlayerSpellState State { get; set; }
    public int? TraitDefinitionId { get; set; }
}