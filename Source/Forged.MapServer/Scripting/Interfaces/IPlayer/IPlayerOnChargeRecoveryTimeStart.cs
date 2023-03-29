﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

public interface IPlayerOnChargeRecoveryTimeStart : IScriptObject, IClassRescriction
{
    void OnChargeRecoveryTimeStart(Player player, uint chargeCategoryId, ref int chargeRecoveryTime);
}