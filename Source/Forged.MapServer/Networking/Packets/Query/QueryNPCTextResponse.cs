// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryNPCTextResponse : ServerPacket
{
    public bool Allow;
    public uint[] BroadcastTextID = new uint[SharedConst.MaxNpcTextOptions];
    public float[] Probabilities = new float[SharedConst.MaxNpcTextOptions];
    public uint TextID;
    public QueryNPCTextResponse() : base(ServerOpcodes.QueryNpcTextResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(TextID);
        WorldPacket.WriteBit(Allow);

        WorldPacket.WriteInt32(Allow ? SharedConst.MaxNpcTextOptions * (4 + 4) : 0);

        if (Allow)
        {
            for (uint i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
                WorldPacket.WriteFloat(Probabilities[i]);

            for (uint i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
                WorldPacket.WriteUInt32(BroadcastTextID[i]);
        }
    }
}