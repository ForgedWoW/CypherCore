// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class RoleChosen : ServerPacket
{
    public bool Accepted;
    public ObjectGuid Player;
    public LfgRoles RoleMask;
    public RoleChosen() : base(ServerOpcodes.RoleChosen) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Player);
        WorldPacket.WriteUInt32((uint)RoleMask);
        WorldPacket.WriteBit(Accepted);
        WorldPacket.FlushBits();
    }
}