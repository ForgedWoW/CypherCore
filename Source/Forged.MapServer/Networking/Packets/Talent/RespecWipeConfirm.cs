// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

internal class RespecWipeConfirm : ServerPacket
{
    public uint Cost;
    public ObjectGuid RespecMaster;
    public SpecResetType RespecType;
    public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm) { }

    public override void Write()
    {
        WorldPacket.WriteInt8((sbyte)RespecType);
        WorldPacket.WriteUInt32(Cost);
        WorldPacket.WritePackedGuid(RespecMaster);
    }
}