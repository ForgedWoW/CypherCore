// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellAuraOptionsRecord
{
    public ushort CumulativeAura;
    public byte DifficultyID;
    public uint Id;
    public uint ProcCategoryRecovery;
    public byte ProcChance;
    public int ProcCharges;
    public int[] ProcTypeMask = new int[2];
    public uint SpellID;
    public ushort SpellProcsPerMinuteID;
}