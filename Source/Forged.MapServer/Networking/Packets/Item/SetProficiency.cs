// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class SetProficiency : ServerPacket
{
    public byte ProficiencyClass;
    public uint ProficiencyMask;
    public SetProficiency() : base(ServerOpcodes.SetProficiency, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(ProficiencyMask);
        _worldPacket.WriteUInt8(ProficiencyClass);
    }
}