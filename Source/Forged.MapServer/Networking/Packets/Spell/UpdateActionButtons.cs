// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class UpdateActionButtons : ServerPacket
{
    public ulong[] ActionButtons = new ulong[PlayerConst.MaxActionButtons];
    public byte Reason;

    public UpdateActionButtons() : base(ServerOpcodes.UpdateActionButtons, ConnectionType.Instance) { }

    public override void Write()
    {
        for (var i = 0; i < PlayerConst.MaxActionButtons; ++i)
            WorldPacket.WriteUInt64(ActionButtons[i]);

        WorldPacket.WriteUInt8(Reason);
    }
}