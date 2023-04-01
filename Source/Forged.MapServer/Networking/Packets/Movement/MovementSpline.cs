// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MovementSpline
{
    public uint Flags;           // Spline flags
    public MonsterMoveType Face; // Movement direction (see MonsterMoveType enum)
    public int Elapsed;
    public uint MoveTime;
    public uint FadeObjectTime;
    public List<Vector3> Points = new(); // Spline path
    public byte Mode;                    // Spline mode - actually always 0 in this packet - Catmullrom mode appears only in SMSG_UPDATE_OBJECT. In this packet it is determined by flags
    public bool VehicleExitVoluntary;
    public bool Interpolate;
    public ObjectGuid TransportGUID;
    public sbyte VehicleSeat = -1;
    public List<Vector3> PackedDeltas = new();
    public MonsterSplineFilter SplineFilter;
    public MonsterSplineSpellEffectExtraData? SpellEffectExtraData;
    public MonsterSplineJumpExtraData? JumpExtraData;
    public MonsterSplineAnimTierTransition? AnimTierTransition;
    public MonsterSplineUnknown901 Unknown901;
    public float FaceDirection;
    public ObjectGuid FaceGUID;
    public Vector3 FaceSpot;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Flags);
        data.WriteInt32(Elapsed);
        data.WriteUInt32(MoveTime);
        data.WriteUInt32(FadeObjectTime);
        data.WriteUInt8(Mode);
        data.WritePackedGuid(TransportGUID);
        data.WriteInt8(VehicleSeat);
        data.WriteBits((byte)Face, 2);
        data.WriteBits(Points.Count, 16);
        data.WriteBit(VehicleExitVoluntary);
        data.WriteBit(Interpolate);
        data.WriteBits(PackedDeltas.Count, 16);
        data.WriteBit(SplineFilter != null);
        data.WriteBit(SpellEffectExtraData.HasValue);
        data.WriteBit(JumpExtraData.HasValue);
        data.WriteBit(AnimTierTransition.HasValue);
        data.WriteBit(Unknown901 != null);
        data.FlushBits();

        SplineFilter?.Write(data);

        switch (Face)
        {
            case MonsterMoveType.FacingSpot:
                data.WriteVector3(FaceSpot);

                break;
            case MonsterMoveType.FacingTarget:
                data.WriteFloat(FaceDirection);
                data.WritePackedGuid(FaceGUID);

                break;
            case MonsterMoveType.FacingAngle:
                data.WriteFloat(FaceDirection);

                break;
        }

        foreach (var pos in Points)
            data.WriteVector3(pos);

        foreach (var pos in PackedDeltas)
            data.WritePackXYZ(pos);

        SpellEffectExtraData?.Write(data);

        JumpExtraData?.Write(data);

        AnimTierTransition?.Write(data);

        Unknown901?.Write(data);
    }
}