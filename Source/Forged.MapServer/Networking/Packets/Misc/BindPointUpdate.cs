// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class BindPointUpdate : ServerPacket
{
    public uint BindAreaID;
    public uint BindMapID = 0xFFFFFFFF;
    public Vector3 BindPosition;
    public BindPointUpdate() : base(ServerOpcodes.BindPointUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteVector3(BindPosition);
        WorldPacket.WriteUInt32(BindMapID);
        WorldPacket.WriteUInt32(BindAreaID);
    }
}

//Structs