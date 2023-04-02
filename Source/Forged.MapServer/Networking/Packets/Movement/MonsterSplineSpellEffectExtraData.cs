// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MonsterSplineSpellEffectExtraData
{
    public float JumpGravity;

    public uint ParabolicCurveID;

    public uint ProgressCurveID;

    public uint SpellVisualID;

    public ObjectGuid TargetGuid;

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(TargetGuid);
        data.WriteUInt32(SpellVisualID);
        data.WriteUInt32(ProgressCurveID);
        data.WriteUInt32(ParabolicCurveID);
        data.WriteFloat(JumpGravity);
    }
}