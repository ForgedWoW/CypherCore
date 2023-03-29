// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

public class EquipmentSetID : ServerPacket
{
    public ulong GUID; // Set Identifier
    public int Type;
    public uint SetID; // Index
    public EquipmentSetID() : base(ServerOpcodes.EquipmentSetId, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(GUID);
        _worldPacket.WriteInt32(Type);
        _worldPacket.WriteUInt32(SetID);
    }
}