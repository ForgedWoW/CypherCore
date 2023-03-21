// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.DataStorage;

public class CharacterTemplate
{
	public uint TemplateSetId;
	public List<CharacterTemplateClass> Classes;
	public string Name;
	public string Description;
	public byte Level;
}