﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ArtifactTierRecord
{
	public uint Id;
	public uint ArtifactTier;
	public uint MaxNumTraits;
	public uint MaxArtifactKnowledge;
	public uint KnowledgePlayerCondition;
	public uint MinimumEmpowerKnowledge;
}