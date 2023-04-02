// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class StartMirrorTimer : ServerPacket
{
    public int MaxValue;
    public bool Paused;
    public int Scale;
    public int SpellID;
    public MirrorTimerType Timer;
    public int Value;
    public StartMirrorTimer(MirrorTimerType timer, int value, int maxValue, int scale, int spellID, bool paused) : base(ServerOpcodes.StartMirrorTimer)
    {
        Timer = timer;
        Value = value;
        MaxValue = maxValue;
        Scale = scale;
        SpellID = spellID;
        Paused = paused;
    }

    public override void Write()
    {
        _worldPacket.WriteInt32((int)Timer);
        _worldPacket.WriteInt32(Value);
        _worldPacket.WriteInt32(MaxValue);
        _worldPacket.WriteInt32(Scale);
        _worldPacket.WriteInt32(SpellID);
        _worldPacket.WriteBit(Paused);
        _worldPacket.FlushBits();
    }
}