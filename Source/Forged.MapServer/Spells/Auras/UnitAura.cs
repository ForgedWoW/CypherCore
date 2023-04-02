// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;

namespace Forged.MapServer.Spells.Auras;

public class UnitAura : Aura
{
    private readonly Dictionary<ObjectGuid, HashSet<int>> _staticApplications = new(); // non-area auras

    private DiminishingGroup _mAuraDrGroup; // Diminishing

    public UnitAura(AuraCreateInfo createInfo) : base(createInfo)
    {
        _mAuraDrGroup = DiminishingGroup.None;
        LoadScripts();
        _InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
        OwnerAsUnit._AddAura(this, createInfo.Caster);
    }

    public override void _ApplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
    {
        base._ApplyForTarget(target, caster, aurApp);

        // register aura diminishing on apply
        if (_mAuraDrGroup != DiminishingGroup.None)
            target.ApplyDiminishingAura(_mAuraDrGroup, true);
    }

    public override void _UnapplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
    {
        base._UnapplyForTarget(target, caster, aurApp);

        // unregister aura diminishing (and store last time)
        if (_mAuraDrGroup != DiminishingGroup.None)
            target.ApplyDiminishingAura(_mAuraDrGroup, false);
    }

    public void AddStaticApplication(Unit target, HashSet<int> effectMask)
    {
        var effMask = effectMask.ToHashSet();

        // only valid for non-area auras
        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (effMask.Contains(spellEffectInfo.EffectIndex) && !spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                effMask.Remove(spellEffectInfo.EffectIndex);

        if (effMask.Count == 0)
            return;

        if (!_staticApplications.ContainsKey(target.GUID))
            _staticApplications[target.GUID] = new HashSet<int>();

        _staticApplications[target.GUID].UnionWith(effMask);
    }

    public override Dictionary<Unit, HashSet<int>> FillTargetMap(Unit caster)
    {
        var targets = new Dictionary<Unit, HashSet<int>>();
        var refe = caster;

        if (refe == null)
            refe = OwnerAsUnit;

        // add non area aura targets
        // static applications go through spell system first, so we assume they meet conditions
        foreach (var targetPair in _staticApplications)
        {
            var target = Global.ObjAccessor.GetUnit(OwnerAsUnit, targetPair.Key);

            if (target == null && targetPair.Key == OwnerAsUnit.GUID)
                target = OwnerAsUnit;

            if (target)
                targets.Add(target, targetPair.Value);
        }

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            if (!HasEffect(spellEffectInfo.EffectIndex))
                continue;

            // area auras only
            if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                continue;

            // skip area update if owner is not in world!
            if (!OwnerAsUnit.Location.IsInWorld)
                continue;

            if (OwnerAsUnit.HasUnitState(UnitState.Isolated))
                continue;

            List<Unit> units = new();
            var condList = spellEffectInfo.ImplicitTargetConditions;

            var radius = spellEffectInfo.CalcRadius(refe);
            var extraSearchRadius = 0.0f;

            var selectionType = SpellTargetCheckTypes.Default;

            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.ApplyAreaAuraParty:
                case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                    selectionType = SpellTargetCheckTypes.Party;

                    break;
                case SpellEffectName.ApplyAreaAuraRaid:
                    selectionType = SpellTargetCheckTypes.Raid;

                    break;
                case SpellEffectName.ApplyAreaAuraFriend:
                    selectionType = SpellTargetCheckTypes.Ally;

                    break;
                case SpellEffectName.ApplyAreaAuraEnemy:
                    selectionType = SpellTargetCheckTypes.Enemy;
                    extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;

                    break;
                case SpellEffectName.ApplyAreaAuraPet:
                    if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(OwnerAsUnit, refe, condList))
                        units.Add(OwnerAsUnit);

                    goto case SpellEffectName.ApplyAreaAuraOwner;
                /* fallthrough */
                case SpellEffectName.ApplyAreaAuraOwner:
                {
                    var owner = OwnerAsUnit.CharmerOrOwner;

                    if (owner != null)
                        if (OwnerAsUnit.Location.IsWithinDistInMap(owner, radius))
                            if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(owner, refe, condList))
                                units.Add(owner);

                    break;
                }
                case SpellEffectName.ApplyAuraOnPet:
                {
                    var pet = Global.ObjAccessor.GetUnit(OwnerAsUnit, OwnerAsUnit.PetGUID);

                    if (pet != null)
                        if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(pet, refe, condList))
                            units.Add(pet);

                    break;
                }
                case SpellEffectName.ApplyAreaAuraSummons:
                {
                    if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(OwnerAsUnit, refe, condList))
                        units.Add(OwnerAsUnit);

                    selectionType = SpellTargetCheckTypes.Summoned;

                    break;
                }
            }

            if (selectionType != SpellTargetCheckTypes.Default)
            {
                WorldObjectSpellAreaTargetCheck check = new(radius, OwnerAsUnit.Location, refe, OwnerAsUnit, SpellInfo, selectionType, condList, SpellTargetObjectTypes.Unit);
                UnitListSearcher searcher = new(OwnerAsUnit, units, check, GridType.All);
                Cell.VisitGrid(OwnerAsUnit, searcher, radius + extraSearchRadius);

                // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
                units.RemoveAll(unit => !unit.Location.IsSelfOrInSameMap(OwnerAsUnit));
            }

            foreach (var unit in units)
            {
                if (!targets.ContainsKey(unit))
                    targets[unit] = new HashSet<int>();

                targets[unit].Add(spellEffectInfo.EffectIndex);
            }
        }

        return targets;
    }

    public DiminishingGroup GetDiminishGroup()
    {
        return _mAuraDrGroup;
    }

    public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (IsRemoved)
            return;

        OwnerAsUnit.RemoveOwnedAura(this, removeMode);
        base.Remove(removeMode);
    }
    // Allow Apply Aura Handler to modify and access m_AuraDRGroup
    public void SetDiminishGroup(DiminishingGroup group)
    {
        _mAuraDrGroup = group;
    }
}