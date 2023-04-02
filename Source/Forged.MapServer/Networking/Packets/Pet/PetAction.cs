// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetAction : ClientPacket
{
    public uint Action;
    public Vector3 ActionPosition;
    public ObjectGuid PetGUID;
    public ObjectGuid TargetGUID;
    public PetAction(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetGUID = WorldPacket.ReadPackedGuid();

        Action = WorldPacket.ReadUInt32();
        TargetGUID = WorldPacket.ReadPackedGuid();

        ActionPosition = WorldPacket.ReadVector3();
    }
}