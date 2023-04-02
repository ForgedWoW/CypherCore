// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class UndeleteCooldownStatusResponse : ServerPacket
{
    public uint CurrentCooldown;
    public uint MaxCooldown;
    public bool OnCooldown;      //
                                 // Max. cooldown until next free character restoration. Displayed in undelete confirm message. (in sec)
                                 // Current cooldown until next free character restoration. (in sec)
    public UndeleteCooldownStatusResponse() : base(ServerOpcodes.UndeleteCooldownStatusResponse) { }

    public override void Write()
    {
        _worldPacket.WriteBit(OnCooldown);
        _worldPacket.WriteUInt32(MaxCooldown);
        _worldPacket.WriteUInt32(CurrentCooldown);
    }
}