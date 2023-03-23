using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellVisualKitRecord
{
	public uint Id;
	public sbyte FallbackPriority;
	public int FallbackSpellVisualKitId;
	public ushort DelayMin;
	public ushort DelayMax;
	public int[] Flags = new int[2];
}
