// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class ModifierTreeRecord
{
    public sbyte Amount;
    public uint Asset;
    public uint Id;
    public sbyte Operator;
    public uint Parent;
    public int SecondaryAsset;
    public int TertiaryAsset;
    public uint Type;
}