// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemDisenchantLootRecord
{
    public uint Class;
    public sbyte ExpansionID;
    public uint Id;
    public ushort MaxLevel;
    public ushort MinLevel;
    public byte Quality;
    public ushort SkillRequired;
    public sbyte Subclass;
}