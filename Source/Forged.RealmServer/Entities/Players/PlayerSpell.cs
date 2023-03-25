// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Entities;

public class PlayerSpell
{
	public PlayerSpellState State;
	public bool Active;
	public bool Dependent;
	public bool Disabled;
	public bool Favorite;
	public int? TraitDefinitionId;
}