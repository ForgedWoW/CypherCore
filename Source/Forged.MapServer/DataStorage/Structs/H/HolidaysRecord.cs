using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.H;

public sealed record HolidaysRecord
{
    public uint Id;
    public ushort Region;
    public byte Looping;
    public uint HolidayNameID;
    public uint HolidayDescriptionID;
    public byte Priority;
    public sbyte CalendarFilterType;
    public byte Flags;
    public ushort[] Duration = new ushort[SharedConst.MaxHolidayDurations];
    public uint[] Date = new uint[SharedConst.MaxHolidayDates]; // dates in unix time starting at January, 1, 2000
    public byte[] CalendarFlags = new byte[SharedConst.MaxHolidayFlags];
    public int[] TextureFileDataID = new int[3];
}