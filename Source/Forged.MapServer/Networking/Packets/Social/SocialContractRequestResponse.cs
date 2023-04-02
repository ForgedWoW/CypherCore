// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Social;

internal class SocialContractRequestResponse : ServerPacket
{
    public bool ShowSocialContract;

    public SocialContractRequestResponse() : base(ServerOpcodes.SocialContractRequestResponse) { }

    public override void Write()
    {
        WorldPacket.WriteBit(ShowSocialContract);
        WorldPacket.FlushBits();
    }
}