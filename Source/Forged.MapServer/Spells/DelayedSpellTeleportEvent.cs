// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Spells;

internal class DelayedSpellTeleportEvent : BasicEvent
{
    private readonly Unit _target;
    private readonly WorldLocation _targetDest;
    private readonly TeleportToOptions _options;
    private readonly uint _spellId;

    public DelayedSpellTeleportEvent(Unit target, WorldLocation targetDest, TeleportToOptions options, uint spellId)
    {
        _target = target;
        _targetDest = targetDest;
        _options = options;
        _spellId = spellId;
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        if (_targetDest.MapId == _target.Location.MapId)
        {
            _target.NearTeleportTo(_targetDest, (_options & TeleportToOptions.Spell) != 0);
        }
        else
        {
            var player = _target.AsPlayer;

            if (player != null)
                player.TeleportTo(_targetDest, _options);
            else
                Log.Logger.Error($"Spell::EffectTeleportUnitsWithVisualLoadingScreen - spellId {_spellId} attempted to teleport creature to a different map.");
        }

        return true;
    }
}