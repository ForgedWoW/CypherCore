// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class AISpellInfoType
{
    public AICondition Condition;
    public TimeSpan Cooldown;
    public byte Effects;
    public float MaxRange;
    public TimeSpan RealCooldown;
    public AITarget Target;
    public byte Targets; // set of enum SelectTarget
     // set of enum SelectEffect

    public AISpellInfoType()
    {
        Target = AITarget.Self;
        Condition = AICondition.Combat;
        Cooldown = TimeSpan.FromMilliseconds(SharedConst.AIDefaultCooldown);
    }
}