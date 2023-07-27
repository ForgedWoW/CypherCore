using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PrestigeLevelInfoRecord
{
    public uint Id;
    public string Name;
    public int PrestigeLevel;
    public int BadgeTextureFileDataID;
    public PrestigeLevelInfoFlags Flags;
    public int AwardedAchievementID;

    public bool IsDisabled() { return (Flags & PrestigeLevelInfoFlags.Disabled) != 0; }
}