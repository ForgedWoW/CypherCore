﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpecializationSpellsRecord
{
	public string Description;
	public uint Id;
	public ushort SpecID;
	public uint SpellID;
	public uint OverridesSpellID;
	public byte DisplayOrder;
}