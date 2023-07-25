using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitDefinitionEffectPointsRecord
{
    public uint Id;
    public int TraitDefinitionID;
    public int EffectIndex;
    public int OperationType;
    public int CurveID;

    public TraitPointsOperationType GetOperationType() { return (TraitPointsOperationType)OperationType; }
}