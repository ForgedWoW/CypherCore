// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Conditions;

namespace Forged.RealmServer.Misc;

public class GossipMenuItems
{
	public uint MenuId { get; set; }
	public int GossipOptionId { get; set; }
	public uint OrderIndex { get; set; }
	public GossipOptionNpc OptionNpc { get; set; }
	public string OptionText { get; set; }
	public uint OptionBroadcastTextId { get; set; }
	public uint Language { get; set; }
	public GossipOptionFlags Flags { get; set; }
	public uint ActionMenuId { get; set; }
	public uint ActionPoiId { get; set; }
	public int? GossipNpcOptionId { get; set; }
	public bool BoxCoded { get; set; }
	public uint BoxMoney { get; set; }
	public string BoxText { get; set; }
	public uint BoxBroadcastTextId { get; set; }
	public int? SpellId { get; set; }
	public int? OverrideIconId { get; set; }
	public List<Condition> Conditions { get; set; } = new();
}