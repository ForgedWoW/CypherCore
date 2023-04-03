// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartTarget
{
    [FieldOffset(0)] public SmartTargets type;

    [FieldOffset(4)] public float x;

    [FieldOffset(8)] public float y;

    [FieldOffset(12)] public float z;

    [FieldOffset(16)] public float o;

    [FieldOffset(20)] public HostilRandom hostilRandom;

    [FieldOffset(20)] public Farthest farthest;

    [FieldOffset(20)] public UnitRange unitRange;

    [FieldOffset(20)] public UnitGUID unitGUID;

    [FieldOffset(20)] public UnitDistance unitDistance;

    [FieldOffset(20)] public PlayerDistance playerDistance;

    [FieldOffset(20)] public PlayerRange playerRange;

    [FieldOffset(20)] public Stored stored;

    [FieldOffset(20)] public GoRange goRange;

    [FieldOffset(20)] public GoGUID goGUID;

    [FieldOffset(20)] public GoDistance goDistance;

    [FieldOffset(20)] public UnitClosest unitClosest;

    [FieldOffset(20)] public GoClosest goClosest;

    [FieldOffset(20)] public ClosestAttackable closestAttackable;

    [FieldOffset(20)] public ClosestFriendly closestFriendly;

    [FieldOffset(20)] public Owner owner;

    [FieldOffset(20)] public Vehicle vehicle;

    [FieldOffset(20)] public ThreatList threatList;

    [FieldOffset(20)] public Raw raw;

    #region Structs

    public struct HostilRandom
    {
        public uint MaxDist;
        public uint PlayerOnly;
        public uint PowerType;
    }

    public struct Farthest
    {
        public uint MaxDist;
        public uint PlayerOnly;
        public uint IsInLos;
    }

    public struct UnitRange
    {
        public uint Creature;
        public uint MinDist;
        public uint MaxDist;
        public uint MaxSize;
    }

    public struct UnitGUID
    {
        public uint DBGuid;
        public uint Entry;
    }

    public struct UnitDistance
    {
        public uint Creature;
        public uint Dist;
        public uint MaxSize;
    }

    public struct PlayerDistance
    {
        public uint Dist;
    }

    public struct PlayerRange
    {
        public uint MinDist;
        public uint MaxDist;
    }

    public struct Stored
    {
        public uint ID;
    }

    public struct GoRange
    {
        public uint Entry;
        public uint MinDist;
        public uint MaxDist;
        public uint MaxSize;
    }

    public struct GoGUID
    {
        public uint DBGuid;
        public uint Entry;
    }

    public struct GoDistance
    {
        public uint Entry;
        public uint Dist;
        public uint MaxSize;
    }

    public struct UnitClosest
    {
        public uint Entry;
        public uint Dist;
        public uint Dead;
    }

    public struct GoClosest
    {
        public uint Entry;
        public uint Dist;
    }

    public struct ClosestAttackable
    {
        public uint MaxDist;
        public uint PlayerOnly;
    }

    public struct ClosestFriendly
    {
        public uint MaxDist;
        public uint PlayerOnly;
    }

    public struct Owner
    {
        public uint UseCharmerOrOwner;
    }

    public struct Vehicle
    {
        public uint SeatMask;
    }

    public struct ThreatList
    {
        public uint MaxDist;
    }

    public struct Raw
    {
        public uint Param1;
        public uint Param2;
        public uint Param3;
        public uint Param4;
    }

    #endregion
}