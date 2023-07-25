using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed class DifficultyRecord
{
    public uint Id;
    public string Name;
    public MapTypes InstanceType;
    public byte OrderIndex;
    public sbyte OldEnumValue;
    public byte FallbackDifficultyID;
    public byte MinPlayers;
    public byte MaxPlayers;
    public DifficultyFlags Flags;
    public byte ItemContext;
    public byte ToggleDifficultyID;
    public uint GroupSizeHealthCurveID;
    public uint GroupSizeDmgCurveID;
    public uint GroupSizeSpellPointsCurveID;
}