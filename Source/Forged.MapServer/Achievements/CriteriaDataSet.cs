// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Achievements;

public class CriteriaDataSet
{
    private readonly CriteriaDataValidator _dataValidator;
    private readonly List<CriteriaData> _storage = new();
    private uint _criteriaId;

    public CriteriaDataSet(CriteriaDataValidator dataValidator)
    {
        _dataValidator = dataValidator;
    }

    public void Add(CriteriaData data)
    {
        _storage.Add(data);
    }

    public bool Meets(Player source, WorldObject target, uint miscValue = 0, uint miscValue2 = 0)
    {
        return _storage.All(data => _dataValidator.Meets(data, _criteriaId, source, target, miscValue, miscValue2));
    }

    public void SetCriteriaId(uint id)
    {
        _criteriaId = id;
    }
}