namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AuctionHouseRecord
{
    public uint Id;
    public string Name;
    public ushort FactionID; // id of faction.dbc for player factions associated with city
    public byte DepositRate;
    public byte ConsignmentRate;
}