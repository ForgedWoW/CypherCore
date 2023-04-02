// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class Response : ServerPacket
{
    public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
    public ByteBuffer Data = new();
    public MethodCall Method;
    public Response() : base(ServerOpcodes.BattlenetResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)BnetStatus);
        Method.Write(WorldPacket);
        WorldPacket.WriteUInt32(Data.GetSize());
        WorldPacket.WriteBytes(Data);
    }
}