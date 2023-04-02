// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureAddon
{
    public ushort AiAnimKit;
    public byte AnimTier;
    public List<uint> Auras = new();
    public uint Emote;
    public ushort MeleeAnimKit;
    public uint Mount;
    public ushort MovementAnimKit;
    public uint PathId;
    public byte PvpFlags;
    public byte SheathState;
    public byte StandState;
    public byte VisFlags;
    public VisibilityDistanceType VisibilityDistanceType;
}