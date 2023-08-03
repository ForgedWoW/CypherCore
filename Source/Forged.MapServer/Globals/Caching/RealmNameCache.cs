// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class RealmNameCache : IObjectCache
{
    private readonly LoginDatabase _loginDatabase;
    private readonly Dictionary<uint, string> _realmNameStorage = new();

    public RealmNameCache(LoginDatabase loginDatabase)
    {
        _loginDatabase = loginDatabase;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;
        _realmNameStorage.Clear();

        //                                         0   1
        var result = _loginDatabase.Query("SELECT id, name FROM `realmlist`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 realm names. DB table `realmlist` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var realm = result.Read<uint>(0);
            var realmName = result.Read<string>(1);

            _realmNameStorage[realm] = realmName;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} realm names in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public string GetRealmName(uint realm)
    {
        return _realmNameStorage.LookupByKey(realm);
    }

    public bool GetRealmName(uint realmId, ref string name, ref string normalizedName)
    {
        if (!_realmNameStorage.TryGetValue(realmId, out var realmName))
            return false;

        name = realmName;
        normalizedName = realmName.Normalize();

        return true;
    }
}