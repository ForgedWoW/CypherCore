// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals;

public sealed class SpellClickInfoObjectManager
{
    private readonly WorldDatabase _worldDatabase;
    private readonly SpellManager _spellManager;
    private GameObjectManager _gameObjectManager;
    private readonly MultiMap<uint, SpellClickInfo> _spellClickInfoStorage = new();

    public SpellClickInfoObjectManager(WorldDatabase worldDatabase, SpellManager spellManager)
    {
        _worldDatabase = worldDatabase;
        _spellManager = spellManager;
    }

    public List<SpellClickInfo> GetSpellClickInfoMapBounds(uint creatureID)
    {
        return _spellClickInfoStorage.LookupByKey(creatureID);
    }

    public void LoadNPCSpellClickSpells()
    {
        var oldMSTime = Time.MSTime;

        _spellClickInfoStorage.Clear();
        //                                           0          1         2            3
        var result = _worldDatabase.Query("SELECT npc_entry, spell_id, cast_flags, user_type FROM npc_spellclick_spells");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spellclick spells. DB table `npc_spellclick_spells` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var npcEntry = result.Read<uint>(0);
            var cInfo = _gameObjectManager.GetCreatureTemplate(npcEntry);

            if (cInfo == null)
            {
                Log.Logger.Error("Table npc_spellclick_spells references unknown creature_template {0}. Skipping entry.", npcEntry);

                continue;
            }

            var spellid = result.Read<uint>(1);
            var spellinfo = _spellManager.GetSpellInfo(spellid);

            if (spellinfo == null)
            {
                Log.Logger.Error("Table npc_spellclick_spells creature: {0} references unknown spellid {1}. Skipping entry.", npcEntry, spellid);

                continue;
            }

            var userType = (SpellClickUserTypes)result.Read<byte>(3);

            if (userType >= SpellClickUserTypes.Max)
                Log.Logger.Error("Table npc_spellclick_spells creature: {0} references unknown user type {1}. Skipping entry.", npcEntry, userType);

            var castFlags = result.Read<byte>(2);

            SpellClickInfo info = new()
            {
                SpellId = spellid,
                CastFlags = castFlags,
                UserType = userType
            };

            _spellClickInfoStorage.Add(npcEntry, info);

            ++count;
        } while (result.NextRow());

        // all spellclick data loaded, now we check if there are creatures with NPC_FLAG_SPELLCLICK but with no data
        // NOTE: It *CAN* be the other way around: no spellclick Id but with spellclick data, in case of creature-only vehicle accessories
        var ctc = _gameObjectManager.CreatureTemplates;

        foreach (var creature in ctc.Values)
            if (creature.Npcflag.HasAnyFlag((uint)NPCFlags.SpellClick) && !_spellClickInfoStorage.ContainsKey(creature.Entry))
            {
                Log.Logger.Warning("npc_spellclick_spells: Creature template {0} has UNIT_NPC_FLAG_SPELLCLICK but no data in spellclick table! Removing Id", creature.Entry);
                creature.Npcflag &= ~(uint)NPCFlags.SpellClick;
            }

        Log.Logger.Information("Loaded {0} spellclick definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}