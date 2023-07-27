namespace Forged.MapServer.DataStorage.Structs.R;

public sealed record RewardPackRecord
{
    public uint Id;
    public ushort CharTitleID;
    public uint Money;
    public byte ArtifactXPDifficulty;
    public float ArtifactXPMultiplier;
    public byte ArtifactXPCategoryID;
    public uint TreasurePickerID;
}