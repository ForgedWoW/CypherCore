// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Update;

public class UpdateObject : ServerPacket
{
    public byte[] Data;
    public ushort MapID;
    public uint NumObjUpdates;
    public UpdateObject() : base(ServerOpcodes.UpdateObject, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(NumObjUpdates);
        WorldPacket.WriteUInt16(MapID);
        WorldPacket.WriteBytes(Data);
    }
}