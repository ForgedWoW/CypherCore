// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class LoginSetTimeSpeed : ServerPacket
{
    public uint GameTime;
    public int GameTimeHolidayOffset;
    public float NewSpeed;
    public uint ServerTime;
    public int ServerTimeHolidayOffset;
    public LoginSetTimeSpeed() : base(ServerOpcodes.LoginSetTimeSpeed, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedTime(ServerTime);
        _worldPacket.WritePackedTime(GameTime);
        _worldPacket.WriteFloat(NewSpeed);
        _worldPacket.WriteInt32(ServerTimeHolidayOffset);
        _worldPacket.WriteInt32(GameTimeHolidayOffset);
    }
}