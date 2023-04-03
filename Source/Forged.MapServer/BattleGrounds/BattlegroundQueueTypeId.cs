// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds;

public struct BattlegroundQueueTypeId
{
    public ushort BattlemasterListId;
    public byte BgType;
    public bool Rated;
    public byte TeamSize;

    public BattlegroundQueueTypeId(ushort battlemasterListId, byte bgType, bool rated, byte teamSize)
    {
        BattlemasterListId = battlemasterListId;
        BgType = bgType;
        Rated = rated;
        TeamSize = teamSize;
    }

    public static BattlegroundQueueTypeId FromPacked(ulong packedQueueId)
    {
        return new BattlegroundQueueTypeId((ushort)(packedQueueId & 0xFFFF), (byte)((packedQueueId >> 16) & 0xF), ((packedQueueId >> 20) & 1) != 0, (byte)((packedQueueId >> 24) & 0x3F));
    }

    public static bool operator !=(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
    {
        return !(left == right);
    }

    public static bool operator <(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
    {
        if (left.BattlemasterListId != right.BattlemasterListId)
            return left.BattlemasterListId < right.BattlemasterListId;

        if (left.BgType != right.BgType)
            return left.BgType < right.BgType;

        if (left.Rated != right.Rated)
            return (left.Rated ? 1 : 0) < (right.Rated ? 1 : 0);

        return left.TeamSize < right.TeamSize;
    }

    public static bool operator ==(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
    {
        return left.BattlemasterListId == right.BattlemasterListId && left.BgType == right.BgType && left.Rated == right.Rated && left.TeamSize == right.TeamSize;
    }

    public static bool operator >(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
    {
        if (left.BattlemasterListId != right.BattlemasterListId)
            return left.BattlemasterListId > right.BattlemasterListId;

        if (left.BgType != right.BgType)
            return left.BgType > right.BgType;

        if (left.Rated != right.Rated)
            return (left.Rated ? 1 : 0) > (right.Rated ? 1 : 0);

        return left.TeamSize > right.TeamSize;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return BattlemasterListId.GetHashCode() ^ BgType.GetHashCode() ^ Rated.GetHashCode() ^ TeamSize.GetHashCode();
    }

    public ulong GetPacked()
    {
        return (ulong)BattlemasterListId | ((ulong)(BgType & 0xF) << 16) | ((ulong)(Rated ? 1 : 0) << 20) | ((ulong)(TeamSize & 0x3F) << 24) | 0x1F10000000000000;
    }

    public override string ToString()
    {
        return $"{{ BattlemasterListId: {BattlemasterListId}, Type: {BgType}, Rated: {Rated}, TeamSize: {TeamSize} }}";
    }
}