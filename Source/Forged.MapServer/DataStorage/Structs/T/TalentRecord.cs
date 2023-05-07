// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TalentRecord
{
    public byte[] CategoryMask = new byte[2];
    public byte ClassID;
    public byte ColumnIndex;
    public string Description;
    public byte Flags;
    public uint Id;
    public uint OverridesSpellID;
    public ushort SpecID;
    public uint SpellID;
    public byte TierID;
}