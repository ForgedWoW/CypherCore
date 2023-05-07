// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellVisualKitRecord
{
    public ushort DelayMax;
    public ushort DelayMin;
    public sbyte FallbackPriority;
    public int FallbackSpellVisualKitId;
    public int[] Flags = new int[2];
    public uint Id;
}