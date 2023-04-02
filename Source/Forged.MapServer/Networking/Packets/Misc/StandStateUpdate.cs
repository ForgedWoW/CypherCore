// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class StandStateUpdate : ServerPacket
{
    private readonly uint AnimKitID;
    private readonly UnitStandStateType State;

    public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
    {
        State = state;
        AnimKitID = animKitId;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(AnimKitID);
        WorldPacket.WriteUInt8((byte)State);
    }
}