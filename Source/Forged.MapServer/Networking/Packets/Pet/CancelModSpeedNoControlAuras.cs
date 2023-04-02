// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class CancelModSpeedNoControlAuras : ClientPacket
{
    public ObjectGuid TargetGUID;

    public CancelModSpeedNoControlAuras(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TargetGUID = WorldPacket.ReadPackedGuid();
    }
}