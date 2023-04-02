// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class CancelAutoRepeat : ServerPacket
{
    public ObjectGuid Guid;
    public CancelAutoRepeat() : base(ServerOpcodes.CancelAutoRepeat) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Guid);
    }
}