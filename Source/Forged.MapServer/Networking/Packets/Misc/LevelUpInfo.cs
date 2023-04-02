// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class LevelUpInfo : ServerPacket
{
    public uint HealthDelta = 0;
    public uint Level = 0;
    public int NumNewPvpTalentSlots;
    public int NumNewTalents;
    public int[] PowerDelta = new int[(int)PowerType.MaxPerClass];
    public int[] StatDelta = new int[(int)Stats.Max];
    public LevelUpInfo() : base(ServerOpcodes.LevelUpInfo) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(Level);
        _worldPacket.WriteUInt32(HealthDelta);

        foreach (var power in PowerDelta)
            _worldPacket.WriteInt32(power);

        foreach (var stat in StatDelta)
            _worldPacket.WriteInt32(stat);

        _worldPacket.WriteInt32(NumNewTalents);
        _worldPacket.WriteInt32(NumNewPvpTalentSlots);
    }
}