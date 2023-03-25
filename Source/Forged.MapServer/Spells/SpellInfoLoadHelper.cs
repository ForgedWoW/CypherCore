// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellInfoLoadHelper
{
	public SpellAuraOptionsRecord AuraOptions;
	public SpellAuraRestrictionsRecord AuraRestrictions;
	public SpellCastingRequirementsRecord CastingRequirements;
	public SpellCategoriesRecord Categories;
	public SpellClassOptionsRecord ClassOptions;
	public SpellCooldownsRecord Cooldowns;
	public Dictionary<int, SpellEffectRecord> Effects = new();
	public SpellEquippedItemsRecord EquippedItems;
	public SpellInterruptsRecord Interrupts;
	public List<SpellLabelRecord> Labels = new();
	public SpellLevelsRecord Levels;
	public SpellMiscRecord Misc;
	public SpellPowerRecord[] Powers = new SpellPowerRecord[SpellConst.MaxPowersPerSpell];
	public SpellReagentsRecord Reagents;
	public List<SpellReagentsCurrencyRecord> ReagentsCurrency = new();
	public SpellScalingRecord Scaling;
	public SpellShapeshiftRecord Shapeshift;
	public SpellTargetRestrictionsRecord TargetRestrictions;
	public SpellTotemsRecord Totems;
	public List<SpellXSpellVisualRecord> Visuals = new(); // only to group visuals when parsing sSpellXSpellVisualStore, not for loading
	public List<SpellEmpowerStageRecord> EmpowerStages = new();
}