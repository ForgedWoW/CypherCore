// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceSaveCreated : ServerPacket
{
    public bool Gm;
    public InstanceSaveCreated() : base(ServerOpcodes.InstanceSaveCreated) { }

    public override void Write()
    {
        WorldPacket.WriteBit(Gm);
        WorldPacket.FlushBits();
    }
}