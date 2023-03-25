// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities;

struct ServersideSpellName
{
	public SpellNameRecord Name;

	public ServersideSpellName(uint id, string name)
	{
		Name = new SpellNameRecord();
		Name.Name = new LocalizedString();

		Name.Id = id;

		for (Locale i = 0; i < Locale.Total; ++i)
			Name.Name[i] = name;
	}
}