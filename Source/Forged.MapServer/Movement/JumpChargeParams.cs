// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Movement;

public class JumpChargeParams
{
    public float JumpGravity { get; set; }
    public uint? ParabolicCurveId { get; set; }
    public uint? ProgressCurveId { get; set; }
    public float Speed { get; set; }

    public uint? SpellVisualId { get; set; }
    public bool TreatSpeedAsMoveTimeSeconds { get; set; }
}