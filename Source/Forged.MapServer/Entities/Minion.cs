// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Entities;

public class Minion : TempSummon
{
    protected Unit Owner { get; set; }
    private float _followAngle;

    public Minion(SummonPropertiesRecord propertiesRecord, Unit owner, bool isWorldObject, ClassFactory classFactory)
        : base(propertiesRecord, owner, isWorldObject, classFactory)
    {
        Owner = owner;
        UnitTypeMask |= UnitTypeMask.Minion;
        _followAngle = SharedConst.PetFollowAngle;
        // @todo: Find correct way
        InitCharmInfo();
    }

    public override float FollowAngle => _followAngle;

    public bool IsGuardianPet => IsPet || SummonPropertiesRecord is { Control: SummonCategory.Pet };
    public bool IsPetAbomination => Entry == (uint)PetEntry.Abomination;
    public bool IsPetDoomguard => Entry == (uint)PetEntry.Doomguard;
    public bool IsPetFelguard => Entry == (uint)PetEntry.Felguard;
    public bool IsPetFelhunter => Entry == (uint)PetEntry.FelHunter;
    public bool IsPetGhoul => Entry == (uint)PetEntry.Ghoul;
    public bool IsPetImp => Entry == (uint)PetEntry.Imp;
    public bool IsPetSuccubus => Entry == (uint)PetEntry.Succubus;
    public bool IsPetVoidwalker => Entry == (uint)PetEntry.VoidWalker;
    public bool IsSpiritWolf => Entry == (uint)PetEntry.SpiritWolf;
    public override Unit OwnerUnit => Owner;

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nOwner: {(OwnerUnit != null ? OwnerUnit.GUID : "")}";
    }

    public override void InitStats(uint duration)
    {
        base.InitStats(duration);

        ReactState = ReactStates.Passive;

        SetCreatorGUID(OwnerUnit.GUID);
        Faction = OwnerUnit.Faction; // TODO: Is this correct? Overwrite the use of SummonPropertiesFlags::UseSummonerFaction

        OwnerUnit.SetMinion(this, true);
    }

    public override void RemoveFromWorld()
    {
        if (!Location.IsInWorld)
            return;

        OwnerUnit.SetMinion(this, false);
        base.RemoveFromWorld();
    }

    public override void SetDeathState(DeathState s)
    {
        base.SetDeathState(s);

        if (s != DeathState.JustDied || !IsGuardianPet)
            return;

        var owner = OwnerUnit;

        if (owner is not { IsPlayer: true } || owner.MinionGUID != GUID)
            return;

        foreach (var controlled in owner.Controlled.Where(controlled => controlled.Entry == Entry && controlled.IsAlive))
        {
            owner.MinionGUID = controlled.GUID;
            owner.PetGUID = controlled.GUID;
            owner.AsPlayer.CharmSpellInitialize();

            break;
        }
    }

    public void SetFollowAngle(float angle)
    {
        _followAngle = angle;
    }

    // Ghoul may be guardian or pet

    // Sludge Belcher dk talent

    // Spirit wolf from feral spirits
}