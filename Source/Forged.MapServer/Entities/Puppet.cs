// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Entities;

public class Puppet : Minion
{
    public Puppet(SummonPropertiesRecord propertiesRecord, Unit owner, ClassFactory classFactory) : base(propertiesRecord, owner, false, classFactory)
    {
        UnitTypeMask |= UnitTypeMask.Puppet;
    }

    public override void InitStats(uint duration)
    {
        base.InitStats(duration);

        SetLevel(OwnerUnit.Level);
        ReactState = ReactStates.Passive;
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        //check if caster is channelling?
        if (!Location.IsInWorld)
            return;

        if (!IsAlive)
            UnSummon();
        // @todo why long distance .die does not remove it
    }
}