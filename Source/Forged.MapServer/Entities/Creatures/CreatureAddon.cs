// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureAddon
{
    public ushort AiAnimKit { get; set; }
    public byte AnimTier { get; set; }
    public List<uint> Auras { get; set; } = new();
    public uint Emote { get; set; }
    public ushort MeleeAnimKit { get; set; }
    public uint Mount { get; set; }
    public ushort MovementAnimKit { get; set; }
    public uint PathId { get; set; }
    public byte PvpFlags { get; set; }
    public byte SheathState { get; set; }
    public byte StandState { get; set; }
    public byte VisFlags { get; set; }
    public VisibilityDistanceType VisibilityDistanceType { get; set; }
}