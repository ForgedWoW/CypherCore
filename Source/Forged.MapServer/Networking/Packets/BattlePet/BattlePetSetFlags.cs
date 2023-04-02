// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetSetFlags : ClientPacket
{
    public FlagsControlType ControlType;
    public uint Flags;
    public ObjectGuid PetGuid;
    public BattlePetSetFlags(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetGuid = WorldPacket.ReadPackedGuid();
        Flags = WorldPacket.ReadUInt32();
        ControlType = (FlagsControlType)WorldPacket.ReadBits<byte>(2);
    }
}