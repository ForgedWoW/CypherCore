// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Spells.Auras;

public class AuraCreateInfo
{
    public HashSet<int> AuraEffectMask { get; set; }
    public Dictionary<int, double> BaseAmount { get; set; }
    public Unit Caster { get; set; }
    public ObjectGuid CasterGuid { get; set; }
    public ObjectGuid CastItemGuid { get; set; }
    public uint CastItemId { get; set; }
    public int CastItemLevel { get; set; } = -1;
    public bool IsRefresh { get; set; }
    public bool ResetPeriodicTimer { get; set; } = true;
    internal Difficulty CastDifficulty;
    internal ObjectGuid CastId;
    public WorldObject Owner { get; set; }
    public SpellInfo SpellInfo { get; set; }

    internal HashSet<int> TargetEffectMask = new();


    public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, HashSet<int> auraEffMask, WorldObject owner)
    {
        CastId = castId;
        SpellInfo = spellInfo;
        CastDifficulty = castDifficulty;
        AuraEffectMask = auraEffMask;
        Owner = owner;
    }
    public void SetBaseAmount(Dictionary<int, double> bp)
    {
        BaseAmount = bp;
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
}