// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Reputation;

public class InitializeFactions : ServerPacket
{
    public ReputationFlags[] FactionFlags = new ReputationFlags[FactionCount];
    public bool[] FactionHasBonus = new bool[FactionCount];
    public int[] FactionStandings = new int[FactionCount];
    //@todo: implement faction bonus
    private const ushort FactionCount = 443;

    public InitializeFactions() : base(ServerOpcodes.InitializeFactions, ConnectionType.Instance) { }

    public override void Write()
    {
        for (ushort i = 0; i < FactionCount; ++i)
        {
            _worldPacket.WriteUInt16((ushort)((ushort)FactionFlags[i] & 0xFF));
            _worldPacket.WriteInt32(FactionStandings[i]);
        }

        for (ushort i = 0; i < FactionCount; ++i)
            _worldPacket.WriteBit(FactionHasBonus[i]);

        _worldPacket.FlushBits();
    }
}