// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.W;

public sealed record WorldEffectRecord
{
    public ushort CombatConditionID;
    public uint Id;
    public uint PlayerConditionID;
    public uint QuestFeedbackEffectID;
    public int TargetAsset;
    public byte TargetType;
    public byte WhenToDisplay;
}