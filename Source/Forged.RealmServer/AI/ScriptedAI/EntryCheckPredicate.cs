// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.AI;

public class EntryCheckPredicate : ICheck<ObjectGuid>
{
	readonly uint _entry;

	public EntryCheckPredicate(uint entry)
	{
		_entry = entry;
	}

	public bool Invoke(ObjectGuid guid)
	{
		return guid.Entry == _entry;
	}
}