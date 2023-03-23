// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Common.Entities.Creatures;

public class CreatureAddon
{
	public uint PathId;
	public uint Mount;
	public byte StandState;
	public byte AnimTier;
	public byte SheathState;
	public byte PvpFlags;
	public byte VisFlags;
	public uint Emote;
	public ushort AiAnimKit;
	public ushort MovementAnimKit;
	public ushort MeleeAnimKit;
	public List<uint> Auras = new();
	public VisibilityDistanceType VisibilityDistanceType;
}
