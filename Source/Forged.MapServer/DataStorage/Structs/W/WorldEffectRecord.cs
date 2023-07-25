namespace Forged.MapServer.DataStorage.Structs.W;

public sealed class WorldEffectRecord
{
    public uint Id;
    public uint QuestFeedbackEffectID;
    public byte WhenToDisplay;
    public byte TargetType;
    public int TargetAsset;
    public uint PlayerConditionID;
    public ushort CombatConditionID;
}