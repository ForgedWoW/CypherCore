// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class VoidStorageItem
{
	public ulong ItemId { get; set; }
	public uint ItemEntry { get; set; }
	public ObjectGuid CreatorGuid { get; set; }
	public uint RandomBonusListId { get; set; }
	public uint FixedScalingLevel { get; set; }
	public uint ArtifactKnowledgeLevel { get; set; }
	public ItemContext Context { get; set; }
	public List<uint> BonusListIDs { get; set; } = new();

	public VoidStorageItem(ulong id, uint entry, ObjectGuid creator, uint randomBonusListId, uint fixedScalingLevel, uint artifactKnowledgeLevel, ItemContext context, List<uint> bonuses)
	{
		ItemId = id;
		ItemEntry = entry;
		CreatorGuid = creator;
		RandomBonusListId = randomBonusListId;
		FixedScalingLevel = fixedScalingLevel;
		ArtifactKnowledgeLevel = artifactKnowledgeLevel;
		Context = context;

		foreach (var value in bonuses)
			BonusListIDs.Add(value);
	}
}