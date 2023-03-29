// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetSetFlags : ClientPacket
{
    public ObjectGuid PetGuid;
    public uint Flags;
    public FlagsControlType ControlType;
    public BattlePetSetFlags(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetGuid = _worldPacket.ReadPackedGuid();
        Flags = _worldPacket.ReadUInt32();
        ControlType = (FlagsControlType)_worldPacket.ReadBits<byte>(2);
    }
}