// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

internal class UseEquipmentSetResult : ServerPacket
{
    public ulong GUID; //Set Identifier
    public byte Reason;
    public UseEquipmentSetResult() : base(ServerOpcodes.UseEquipmentSetResult) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(GUID);
        _worldPacket.WriteUInt8(Reason);
    }
}