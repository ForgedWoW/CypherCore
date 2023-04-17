// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

public class EquipmentSetID : ServerPacket
{
    public ulong GUID; // Set Identifier
    public uint SetID;

    public int Type;

    // Index
    public EquipmentSetID() : base(ServerOpcodes.EquipmentSetId, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(GUID);
        WorldPacket.WriteInt32(Type);
        WorldPacket.WriteUInt32(SetID);
    }
}