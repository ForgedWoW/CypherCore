using System;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemLevelSelectorQualityRecord : IEquatable<ItemLevelSelectorQualityRecord>, IEquatable<ItemQuality>
{
    public uint Id;
    public uint QualityItemBonusListID;
    public sbyte Quality;
    public uint ParentILSQualitySetID;

    public bool Equals(ItemLevelSelectorQualityRecord other) { return Quality < other.Quality; }

    public bool Equals(ItemQuality quality) { return Quality < (sbyte)quality; }
}