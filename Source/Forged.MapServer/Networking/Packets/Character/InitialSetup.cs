// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class InitialSetup : ServerPacket
{
    public byte ServerExpansionLevel;
    public byte ServerExpansionTier;
    public InitialSetup() : base(ServerOpcodes.InitialSetup, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt8(ServerExpansionLevel);
        _worldPacket.WriteUInt8(ServerExpansionTier);
    }
}