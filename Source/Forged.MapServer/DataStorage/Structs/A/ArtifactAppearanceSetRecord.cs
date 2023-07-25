namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class ArtifactAppearanceSetRecord
{
    public string Name;
    public string Description;
    public uint Id;
    public byte DisplayIndex;
    public ushort UiCameraID;
    public ushort AltHandUICameraID;
    public sbyte ForgeAttachmentOverride;
    public byte Flags;
    public uint ArtifactID;
}