// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureFamilyRecord
{
    public int IconFileID;
    public uint Id;
    public float MaxScale;
    public sbyte MaxScaleLevel;
    public float MinScale;
    public sbyte MinScaleLevel;
    public LocalizedString Name;
    public ushort PetFoodMask;
    public sbyte PetTalentType;
    public short[] SkillLine = new short[2];
}