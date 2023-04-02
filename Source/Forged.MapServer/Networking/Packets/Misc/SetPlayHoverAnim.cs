// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class SetPlayHoverAnim : ServerPacket
{
    public bool PlayHoverAnim;
    public ObjectGuid UnitGUID;
    public SetPlayHoverAnim() : base(ServerOpcodes.SetPlayHoverAnim, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteBit(PlayHoverAnim);
        WorldPacket.FlushBits();
    }
}