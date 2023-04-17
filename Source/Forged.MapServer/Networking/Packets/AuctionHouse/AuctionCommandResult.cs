// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionCommandResult : ServerPacket
{
    public uint AuctionID;

    /// < the error code that was generated when trying to perform the action. Possible values are @ ref AuctionError
    public int BagResult;

    ///< the id of the auction that triggered this notification
    public int Command;

    ///< the amount of money that the player bid in copper
    public uint DesiredDelay;

    /// < the type of action that triggered this notification. Possible values are @ ref AuctionAction
    public int ErrorCode;

    /// < the bid error. Possible values are @ ref AuctionError
    public ObjectGuid Guid;

    ///< the GUID of the bidder for this auction.
    public ulong MinIncrement;

    ///< the sum of outbid is (1% of current bid) * 5, if the bid is too small, then this value is 1 copper.
    public ulong Money;

    public AuctionCommandResult() : base(ServerOpcodes.AuctionCommandResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(AuctionID);
        WorldPacket.WriteInt32(Command);
        WorldPacket.WriteInt32(ErrorCode);
        WorldPacket.WriteInt32(BagResult);
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WriteUInt64(MinIncrement);
        WorldPacket.WriteUInt64(Money);
        WorldPacket.WriteUInt32(DesiredDelay);
    }
}