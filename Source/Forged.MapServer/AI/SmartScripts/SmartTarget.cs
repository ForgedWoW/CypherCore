// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartTarget
{
    [FieldOffset(0)] public SmartTargets Type;

    [FieldOffset(4)] public float X;

    [FieldOffset(8)] public float Y;

    [FieldOffset(12)] public float Z;

    [FieldOffset(16)] public float O;

    [FieldOffset(20)] public SmartTargetHostilRandom HostilRandom;

    [FieldOffset(20)] public SmartTargetFarthest Farthest;

    [FieldOffset(20)] public SmartTargetUnitRange UnitRange;

    [FieldOffset(20)] public SmartTargetUnitGUID UnitGUID;

    [FieldOffset(20)] public SmartTargetUnitDistance UnitDistance;

    [FieldOffset(20)] public SmartTargetPlayerDistance PlayerDistance;

    [FieldOffset(20)] public SmartTargetPlayerRange PlayerRange;

    [FieldOffset(20)] public SmartTargetStored Stored;

    [FieldOffset(20)] public SmartTargetGoRange GoRange;

    [FieldOffset(20)] public SmartTargetGoGUID GoGUID;

    [FieldOffset(20)] public SmartTargetGoDistance GoDistance;

    [FieldOffset(20)] public SmartTargetUnitClosest UnitClosest;

    [FieldOffset(20)] public SmartTargetGoClosest GoClosest;

    [FieldOffset(20)] public SmartTargetClosestAttackable ClosestAttackable;

    [FieldOffset(20)] public SmartTargetClosestFriendly ClosestFriendly;

    [FieldOffset(20)] public SmartTargetOwner Owner;

    [FieldOffset(20)] public SmartTargetVehicle Vehicle;

    [FieldOffset(20)] public SmartTargetThreatList ThreatList;

    [FieldOffset(20)] public SmartTargetRaw Raw;

    #region Structs

    public struct SmartTargetHostilRandom
    {
        public uint MaxDist;
        public uint PlayerOnly;
        public uint PowerType;
    }

    public struct SmartTargetFarthest
    {
        public uint MaxDist;
        public uint PlayerOnly;
        public uint IsInLos;
    }

    public struct SmartTargetUnitRange
    {
        public uint Creature;
        public uint MinDist;
        public uint MaxDist;
        public uint MaxSize;
    }

    public struct SmartTargetUnitGUID
    {
        public uint DBGuid;
        public uint Entry;
    }

    public struct SmartTargetUnitDistance
    {
        public uint Creature;
        public uint Dist;
        public uint MaxSize;
    }

    public struct SmartTargetPlayerDistance
    {
        public uint Dist;
    }

    public struct SmartTargetPlayerRange
    {
        public uint MinDist;
        public uint MaxDist;
    }

    public struct SmartTargetStored
    {
        public uint ID;
    }

    public struct SmartTargetGoRange
    {
        public uint Entry;
        public uint MinDist;
        public uint MaxDist;
        public uint MaxSize;
    }

    public struct SmartTargetGoGUID
    {
        public uint DBGuid;
        public uint Entry;
    }

    public struct SmartTargetGoDistance
    {
        public uint Entry;
        public uint Dist;
        public uint MaxSize;
    }

    public struct SmartTargetUnitClosest
    {
        public uint Entry;
        public uint Dist;
        public uint Dead;
    }

    public struct SmartTargetGoClosest
    {
        public uint Entry;
        public uint Dist;
    }

    public struct SmartTargetClosestAttackable
    {
        public uint MaxDist;
        public uint PlayerOnly;
    }

    public struct SmartTargetClosestFriendly
    {
        public uint MaxDist;
        public uint PlayerOnly;
    }

    public struct SmartTargetOwner
    {
        public uint UseCharmerOrOwner;
    }

    public struct SmartTargetVehicle
    {
        public uint SeatMask;
    }

    public struct SmartTargetThreatList
    {
        public uint MaxDist;
    }

    public struct SmartTargetRaw
    {
        public uint Param1;
        public uint Param2;
        public uint Param3;
        public uint Param4;
    }

    #endregion
}