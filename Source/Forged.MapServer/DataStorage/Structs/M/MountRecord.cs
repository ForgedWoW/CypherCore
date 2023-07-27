using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MountRecord
{
    public string Name;
    public string SourceText;
    public string Description;
    public uint Id;
    public ushort MountTypeID;
    public MountFlags Flags;
    public sbyte SourceTypeEnum;
    public uint SourceSpellID;
    public uint PlayerConditionID;
    public float MountFlyRideHeight;
    public int UiModelSceneID;
    public int MountSpecialRiderAnimKitID;
    public int MountSpecialSpellVisualKitID;

    public bool IsSelfMount() { return (Flags & MountFlags.SelfMount) != 0; }
}