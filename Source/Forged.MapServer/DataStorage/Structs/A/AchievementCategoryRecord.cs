using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AchievementCategoryRecord
{
    public LocalizedString Name;
    public uint Id;
    public short Parent;
    public sbyte UiOrder;
}