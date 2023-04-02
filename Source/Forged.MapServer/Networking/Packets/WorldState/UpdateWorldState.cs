// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.WorldState;

public class UpdateWorldState : ServerPacket
{
    public bool Hidden;
    public int Value;
    // @todo: research
    public uint VariableID;
    public UpdateWorldState() : base(ServerOpcodes.UpdateWorldState, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(VariableID);
        WorldPacket.WriteInt32(Value);
        WorldPacket.WriteBit(Hidden);
        WorldPacket.FlushBits();
    }
}