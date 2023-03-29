// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class StartTimer : ServerPacket
{
    public uint TotalTime;
    public uint TimeLeft;
    public TimerType Type;
    public StartTimer() : base(ServerOpcodes.StartTimer) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(TotalTime);
        _worldPacket.WriteUInt32(TimeLeft);
        _worldPacket.WriteInt32((int)Type);
    }
}