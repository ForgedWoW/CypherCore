using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LanguagesRecord
{
    public LocalizedString Name;
    public uint Id;
    public int Flags;
    public int UiTextureKitID;
    public int UiTextureKitElementCount;
    public int LearningCurveID;
}