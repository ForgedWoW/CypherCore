// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public class MountCache
{
    public readonly Dictionary<uint, uint> FactionSpecificMounts = new();
    private readonly DB2Manager _db2Manager;
    private readonly WorldDatabase _worldDatabase;
    public MountCache(WorldDatabase worldDatabase, DB2Manager db2Manager)
    {
        _worldDatabase = worldDatabase;
        _db2Manager = db2Manager;
    }

    public void LoadMountDefinitions()
    {
        var oldMsTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT spellId, otherFactionSpellId FROM mount_definitions");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 mount definitions. DB table `mount_definitions` is empty.");

            return;
        }

        do
        {
            var spellId = result.Read<uint>(0);
            var otherFactionSpellId = result.Read<uint>(1);

            if (_db2Manager.GetMount(spellId) == null)
            {
                Log.Logger.Error("Mount spell {0} defined in `mount_definitions` does not exist in Mount.db2, skipped", spellId);

                continue;
            }

            if (otherFactionSpellId != 0 && _db2Manager.GetMount(otherFactionSpellId) == null)
            {
                Log.Logger.Error("otherFactionSpellId {0} defined in `mount_definitions` for spell {1} does not exist in Mount.db2, skipped", otherFactionSpellId, spellId);

                continue;
            }

            FactionSpecificMounts[spellId] = otherFactionSpellId;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} mount definitions in {1} ms", FactionSpecificMounts.Count, Time.GetMSTimeDiffToNow(oldMsTime));
    }
}