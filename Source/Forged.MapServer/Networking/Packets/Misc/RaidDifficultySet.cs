// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class RaidDifficultySet : ServerPacket
{
    public int DifficultyID;
    public bool Legacy;
    public RaidDifficultySet() : base(ServerOpcodes.RaidDifficultySet) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(DifficultyID);
        _worldPacket.WriteUInt8((byte)(Legacy ? 1 : 0));
    }
}