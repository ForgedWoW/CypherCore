// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveSplineSetFlag : ServerPacket
{
    public ObjectGuid MoverGUID;
    public MoveSplineSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(MoverGUID);
    }
}