// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AuctionHouseRecord
{
    public byte ConsignmentRate;
    public byte DepositRate;
    public ushort FactionID;
    public uint Id;
    public string Name;
     // id of faction.dbc for player factions associated with city
}