// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class Minion : TempSummon
{
    protected Unit Owner;
    private float _followAngle;

    public bool IsGuardianPet => IsPet || SummonPropertiesRecord is { Control: SummonCategory.Pet };

    public override float FollowAngle
    {
        get { return _followAngle; }
    }

    public override Unit OwnerUnit => Owner;

    public Minion(SummonPropertiesRecord propertiesRecord, Unit owner, bool isWorldObject)
        : base(propertiesRecord, owner, isWorldObject)
    {
        Owner = owner;
        UnitTypeMask |= UnitTypeMask.Minion;
        _followAngle = SharedConst.PetFollowAngle;
        /// @todo: Find correct way
        InitCharmInfo();
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
        if (!IsInWorld)
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

        if (owner == null || !owner.IsPlayer || owner.MinionGUID != GUID)
            return;

        foreach (var controlled in owner.Controlled)
            if (controlled.Entry == Entry && controlled.IsAlive)
            {
                owner.MinionGUID = controlled.GUID;
                owner.PetGUID = controlled.GUID;
                owner.AsPlayer.CharmSpellInitialize();

                break;
            }
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nOwner: {(OwnerUnit ? OwnerUnit.GUID : "")}";
    }

    public void SetFollowAngle(float angle)
    {
        _followAngle = angle;
    }

    // Warlock pets
    public bool IsPetImp()
    {
        return Entry == (uint)PetEntry.Imp;
    }

    public bool IsPetFelhunter()
    {
        return Entry == (uint)PetEntry.FelHunter;
    }

    public bool IsPetVoidwalker()
    {
        return Entry == (uint)PetEntry.VoidWalker;
    }

    public bool IsPetSuccubus()
    {
        return Entry == (uint)PetEntry.Succubus;
    }

    public bool IsPetDoomguard()
    {
        return Entry == (uint)PetEntry.Doomguard;
    }

    public bool IsPetFelguard()
    {
        return Entry == (uint)PetEntry.Felguard;
    }

    // Death Knight pets
    public bool IsPetGhoul()
    {
        return Entry == (uint)PetEntry.Ghoul;
    } // Ghoul may be guardian or pet

    public bool IsPetAbomination()
    {
        return Entry == (uint)PetEntry.Abomination;
    } // Sludge Belcher dk talent

    // Shaman pet
    public bool IsSpiritWolf()
    {
        return Entry == (uint)PetEntry.SpiritWolf;
    } // Spirit wolf from feral spirits
}