// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public enum PvpMatchState : byte
{
    Waiting = 0,
    StartUp = 1,
    Engaged = 2,
    PostRound = 3,
    Inactive = 4,
    Complete = 5
};

public class PVPMatchSetState : ServerPacket
{
    public PvpMatchState State;

    public PVPMatchSetState(PvpMatchState state) : base(ServerOpcodes.PvpMatchInitialize, ConnectionType.Instance)
    {
        State = state;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)State);
    }
}