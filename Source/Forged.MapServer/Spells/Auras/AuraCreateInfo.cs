// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Spells.Auras;

public class AuraCreateInfo
{
    public HashSet<int> AuraEffectMask = new();
    public Dictionary<int, double> BaseAmount;
    public Unit Caster;
    public ObjectGuid CasterGuid;
    public ObjectGuid CastItemGuid;
    public uint CastItemId = 0;
    public int CastItemLevel = -1;
    public bool IsRefresh;
    public bool ResetPeriodicTimer = true;
    internal Difficulty CastDifficulty;
    internal ObjectGuid CastId;
    internal WorldObject OwnerInternal;
    internal SpellInfo SpellInfoInternal;
    internal HashSet<int> TargetEffectMask = new();


    public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, HashSet<int> auraEffMask, WorldObject owner)
    {
        CastId = castId;
        SpellInfoInternal = spellInfo;
        CastDifficulty = castDifficulty;
        AuraEffectMask = auraEffMask;
        OwnerInternal = owner;
    }

    public WorldObject Owner => OwnerInternal;
    public SpellInfo SpellInfo => SpellInfoInternal;
    public void SetBaseAmount(Dictionary<int, double> bp)
    {
        BaseAmount = bp;
    }

    public void SetCaster(Unit caster)
    {
        Caster = caster;
    }

    public void SetCasterGuid(ObjectGuid guid)
    {
        CasterGuid = guid;
    }
    public void SetCastItem(ObjectGuid guid, uint itemId, int itemLevel)
    {
        CastItemGuid = guid;
        CastItemId = itemId;
        CastItemLevel = itemLevel;
    }

    public void SetOwnerEffectMask(HashSet<int> effMask)
    {
        TargetEffectMask = effMask;
    }

    public void SetPeriodicReset(bool reset)
    {
        ResetPeriodicTimer = reset;
    }
}