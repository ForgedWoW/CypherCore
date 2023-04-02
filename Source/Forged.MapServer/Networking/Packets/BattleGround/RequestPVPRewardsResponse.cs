// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class RequestPVPRewardsResponse : ServerPacket
{
    public uint ArenaMaxRewardPointsThisWeek;
    public uint ArenaRewardPoints;
    public uint ArenaRewardPointsThisWeek;
    public uint MaxRewardPointsThisWeek;
    public uint RandomMaxRewardPointsThisWeek;
    public uint RandomRewardPointsThisWeek;
    public uint RatedMaxRewardPointsThisWeek;
    public uint RatedRewardPoints;
    public uint RatedRewardPointsThisWeek;
    public uint RewardPointsThisWeek;
    public RequestPVPRewardsResponse() : base(ServerOpcodes.RequestPvpRewardsResponse) { }

    public override void Write()
    {
        throw new NotImplementedException();
    }
}