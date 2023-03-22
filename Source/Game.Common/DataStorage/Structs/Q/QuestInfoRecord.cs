// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class QuestInfoRecord
{
	public uint Id;
	public LocalizedString InfoName;
	public sbyte Type;
	public int Modifiers;
	public int Profession;
}