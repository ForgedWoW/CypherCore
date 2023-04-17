// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MovementSpline
{
    public MonsterSplineAnimTierTransition? AnimTierTransition;
    public int Elapsed;
    public MonsterMoveType Face;
    public float FaceDirection;
    public ObjectGuid FaceGUID;
    public Vector3 FaceSpot;
    public uint FadeObjectTime;
    public uint Flags; // Spline flags
    public bool Interpolate;

    public MonsterSplineJumpExtraData? JumpExtraData;

    public byte Mode;

    // Movement direction (see MonsterMoveType enum)
    public uint MoveTime;
    public List<Vector3> PackedDeltas = new();
    public List<Vector3> Points = new(); // Spline path
    public MonsterSplineSpellEffectExtraData? SpellEffectExtraData;

    public MonsterSplineFilter SplineFilter;

    public ObjectGuid TransportGUID;

    public MonsterSplineUnknown901 Unknown901;

    // Spline mode - actually always 0 in this packet - Catmullrom mode appears only in SMSG_UPDATE_OBJECT. In this packet it is determined by flags
    public bool VehicleExitVoluntary;
    public sbyte VehicleSeat = -1;

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