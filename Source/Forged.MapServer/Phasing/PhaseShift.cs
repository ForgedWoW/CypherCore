// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;

namespace Forged.MapServer.Phasing;

public class PhaseShift
{
    private int _cosmeticReferences;
    private int _defaultReferences;
    private int _nonCosmeticReferences;

    public PhaseShift()
    {
        Flags = PhaseShiftFlags.Unphased;
    }

    public PhaseShift(PhaseShift copy)
    {
        Flags = copy.Flags;
        PersonalGuid = copy.PersonalGuid;
        Phases = new Dictionary<uint, PhaseRef>(copy.Phases);
        VisibleMapIds = new Dictionary<uint, VisibleMapIdRef>(copy.VisibleMapIds);
        UiMapPhaseIds = new Dictionary<uint, UiMapPhaseIdRef>(copy.UiMapPhaseIds);

        _nonCosmeticReferences = copy._nonCosmeticReferences;
        _cosmeticReferences = copy._cosmeticReferences;
        _defaultReferences = copy._defaultReferences;
        IsDbPhaseShift = copy.IsDbPhaseShift;
    }

    public PhaseShiftFlags Flags { get; set; }

    public bool HasPersonalPhase
    {
        get { return Phases.Values.Any(phaseRef => phaseRef.IsPersonal()); }
    }

    public bool IsDbPhaseShift { get; set; }
    public ObjectGuid PersonalGuid { get; set; }
    public int PersonalReferences { get; set; }
    public Dictionary<uint, PhaseRef> Phases { get; set; } = new();
    public Dictionary<uint, UiMapPhaseIdRef> UiMapPhaseIds { get; set; } = new();
    public Dictionary<uint, VisibleMapIdRef> VisibleMapIds { get; set; } = new();

    public bool AddPhase(uint phaseId, PhaseFlags flags, List<Condition> areaConditions, int references = 1)
    {
        var newPhase = false;

        if (!Phases.ContainsKey(phaseId))
        {
            newPhase = true;
            Phases.Add(phaseId, new PhaseRef(flags, null));
        }

        var phase = Phases.LookupByKey(phaseId);
        ModifyPhasesReferences(phaseId, phase, references);

        if (areaConditions != null)
            phase.AreaConditions = areaConditions;

        return newPhase;
    }

    public bool AddUiMapPhaseId(uint uiMapPhaseId, int references = 1)
    {
        if (UiMapPhaseIds.ContainsKey(uiMapPhaseId))
            return false;

        UiMapPhaseIds.Add(uiMapPhaseId, new UiMapPhaseIdRef(references));

        return true;
    }

    public bool AddVisibleMapId(uint visibleMapId, TerrainSwapInfo visibleMapInfo, int references = 1)
    {
        if (VisibleMapIds.ContainsKey(visibleMapId))
            return false;

        VisibleMapIds.Add(visibleMapId, new VisibleMapIdRef(references, visibleMapInfo));

        return true;
    }

    public bool CanSee(PhaseShift other)
    {
        if (Flags.HasFlag(PhaseShiftFlags.Unphased) && other.Flags.HasFlag(PhaseShiftFlags.Unphased))
            return true;

        if (Flags.HasFlag(PhaseShiftFlags.AlwaysVisible) || other.Flags.HasFlag(PhaseShiftFlags.AlwaysVisible))
            return true;

        if (Flags.HasFlag(PhaseShiftFlags.Inverse) && other.Flags.HasFlag(PhaseShiftFlags.Inverse))
            return true;

        var excludePhasesWithFlag = PhaseFlags.None;

        if (Flags.HasFlag(PhaseShiftFlags.NoCosmetic) && other.Flags.HasFlag(PhaseShiftFlags.NoCosmetic))
            excludePhasesWithFlag = PhaseFlags.Cosmetic;

        if (!Flags.HasFlag(PhaseShiftFlags.Inverse) && !other.Flags.HasFlag(PhaseShiftFlags.Inverse))
        {
            var ownerGuid = PersonalGuid;
            var otherPersonalGuid = other.PersonalGuid;

            return Phases.Intersect(other.Phases,
                                    (myPhase, otherPhase) =>
                                    {
                                        if (myPhase.Key != otherPhase.Key)
                                            return false;

                                        return !myPhase.Value.Flags.HasAnyFlag(excludePhasesWithFlag) && (!myPhase.Value.Flags.HasFlag(PhaseFlags.Personal) || ownerGuid == otherPersonalGuid);
                                    })
                         .Any();
        }

        var checkInversePhaseShift = new Func<PhaseShift, PhaseShift, bool>((phaseShift, excludedPhaseShift) =>
        {
            if (phaseShift.Flags.HasFlag(PhaseShiftFlags.Unphased) && excludedPhaseShift.Flags.HasFlag(PhaseShiftFlags.InverseUnphased))
                return false;

            foreach (var pair in phaseShift.Phases)
            {
                if (pair.Value.Flags.HasAnyFlag(excludePhasesWithFlag))
                    continue;

                var excludedPhaseRef = excludedPhaseShift.Phases.LookupByKey(pair.Key);

                if (excludedPhaseRef == null || !excludedPhaseRef.Flags.HasAnyFlag(excludePhasesWithFlag))
                    return false;
            }

            return true;
        });

        return other.Flags.HasFlag(PhaseShiftFlags.Inverse) ? checkInversePhaseShift(this, other) : checkInversePhaseShift(other, this);
    }

    public void Clear()
    {
        ClearPhases();
        VisibleMapIds.Clear();
        UiMapPhaseIds.Clear();
    }

    public void ClearPhases()
    {
        Flags &= PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Inverse;
        PersonalGuid.Clear();
        Phases.Clear();
        _nonCosmeticReferences = 0;
        _cosmeticReferences = 0;
        PersonalReferences = 0;
        _defaultReferences = 0;
        UpdateUnphasedFlag();
    }

    public Dictionary<uint, VisibleMapIdRef> GetVisibleMapIds()
    {
        return VisibleMapIds;
    }

    public bool HasPhase(uint phaseId)
    {
        return Phases.ContainsKey(phaseId);
    }

    public bool HasUiWorldMapAreaIdSwap(uint uiWorldMapAreaId)
    {
        return UiMapPhaseIds.ContainsKey(uiWorldMapAreaId);
    }

    public bool HasVisibleMapId(uint visibleMapId)
    {
        return VisibleMapIds.ContainsKey(visibleMapId);
    }

    public void ModifyPhasesReferences(uint phaseId, PhaseRef phaseRef, int references)
    {
        phaseRef.References += references;

        if (!IsDbPhaseShift)
        {
            if (phaseRef.Flags.HasAnyFlag(PhaseFlags.Cosmetic))
                _cosmeticReferences += references;
            else if (phaseId != 169)
                _nonCosmeticReferences += references;
            else
                _defaultReferences += references;

            if (phaseRef.Flags.HasFlag(PhaseFlags.Personal))
                PersonalReferences += references;

            if (_cosmeticReferences != 0)
                Flags |= PhaseShiftFlags.NoCosmetic;
            else
                Flags &= ~PhaseShiftFlags.NoCosmetic;

            UpdateUnphasedFlag();
            UpdatePersonalGuid();
        }
    }

    public bool RemovePhase(uint phaseId)
    {
        if (!Phases.TryGetValue(phaseId, out var phaseRef))
            return false;

        ModifyPhasesReferences(phaseId, phaseRef, -1);

        if (phaseRef.References != 0)
            return false;

        Phases.Remove(phaseId);

        return true;
    }

    public bool RemoveUiMapPhaseId(uint uiWorldMapAreaId)
    {
        if (!UiMapPhaseIds.ContainsKey(uiWorldMapAreaId))
            return false;

        var value = UiMapPhaseIds[uiWorldMapAreaId];

        if (--value.References != 0)
            return false;

        UiMapPhaseIds.Remove(uiWorldMapAreaId);

        return true;
    }

    public bool RemoveVisibleMapId(uint visibleMapId)
    {
        if (!VisibleMapIds.ContainsKey(visibleMapId))
            return false;

        var mapIdRef = VisibleMapIds[visibleMapId];

        if (--mapIdRef.References != 0)
            return false;

        VisibleMapIds.Remove(visibleMapId);

        return true;
    }

    public void UpdateUnphasedFlag()
    {
        var unphasedFlag = !Flags.HasAnyFlag(PhaseShiftFlags.Inverse) ? PhaseShiftFlags.Unphased : PhaseShiftFlags.InverseUnphased;
        Flags &= ~(!Flags.HasFlag(PhaseShiftFlags.Inverse) ? PhaseShiftFlags.InverseUnphased : PhaseShiftFlags.Unphased);

        if (_nonCosmeticReferences != 0 && _defaultReferences == 0)
            Flags &= ~unphasedFlag;
        else
            Flags |= unphasedFlag;
    }

    private void UpdatePersonalGuid()
    {
        if (PersonalReferences == 0)
            PersonalGuid.Clear();
    }
}