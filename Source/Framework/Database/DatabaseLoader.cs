// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Serilog;

namespace Framework.Database;

public class DatabaseLoader
{
    private readonly bool _autoSetup;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly List<Func<bool>> _open = new();
    private readonly List<Func<bool>> _populate = new();
    private readonly List<Func<bool>> _prepare = new();
    private readonly List<Func<bool>> _update = new();
    private readonly DatabaseTypeFlags _updateFlags;

    public DatabaseLoader(DatabaseTypeFlags defaultUpdateMask, IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
        _autoSetup = _configuration.GetDefaultValue("Updates.AutoSetup", true);
        _updateFlags = _configuration.GetDefaultValue("Updates.EnableDatabases", defaultUpdateMask);
    }

    public void AddDatabase<T>(MySqlBase<T> database, string baseDBName)
    {
        var updatesEnabled = database.IsAutoUpdateEnabled(_updateFlags);

        _open.Add(() =>
        {
            MySqlConnectionInfo connectionObject = new()
            {
                Host = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.Host", ""),
                PortOrSocket = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.Port", ""),
                Username = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.Username", ""),
                Password = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.Password", ""),
                Database = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.Database", ""),
                UseSSL = _configuration.GetDefaultValue(baseDBName + "DatabaseInfo.SSL", false)
            };

            var error = database.Initialize(connectionObject);

            if (error != MySqlErrorCode.None)
            {
                // Database does not exist
                if (error == MySqlErrorCode.UnknownDatabase && updatesEnabled && _autoSetup)
                    // Try to create the database and connect again if auto setup is enabled
                    if (CreateDatabase(connectionObject, database))
                        error = database.Initialize(connectionObject);

                // If the error wasn't handled quit
                if (error != MySqlErrorCode.None)
                {
                    _logger.Error($"\nDatabase {connectionObject.Database} NOT opened. There were errors opening the MySQL connections. Check your SQLErrors for specific errors.");

                    return false;
                }
            }

            return true;
        });

        if (updatesEnabled)
        {
            // Populate and update only if updates are enabled for this pool
            _populate.Add(() =>
            {
                if (!database.GetUpdater().Populate())
                {
                    _logger.Error($"Could not populate the {database.GetDatabaseName()} database, see log for details.");

                    return false;
                }

                return true;
            });

            _update.Add(() =>
            {
                if (!database.GetUpdater().Update())
                {
                    _logger.Error($"Could not update the {database.GetDatabaseName()} database, see log for details.");

                    return false;
                }

                return true;
            });
        }

        _prepare.Add(() =>
        {
            database.LoadPreparedStatements();

            return true;
        });
    }

    public bool CreateDatabase<T>(MySqlConnectionInfo connectionObject, MySqlBase<T> database)
    {
        _logger.Information($"Database \"{connectionObject.Database}\" does not exist, do you want to create it? [yes (default) / no]: ");

        var answer = Console.ReadLine();

        if (!answer.IsEmpty() && answer[0] != 'y')
            return false;

        _logger.Information($"Creating database \"{connectionObject.Database}\"...");

        // Path of temp file
        var temp = "create_table.sql";

        // Create temporary query to use external MySQL CLi
        try
        {
            using StreamWriter streamWriter = new(File.Open(temp, FileMode.Create, FileAccess.Write));
            streamWriter.Write($"CREATE DATABASE `{connectionObject.Database}` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        }
        catch (Exception)
        {
            _logger.Fatal($"Failed to create temporary query file \"{temp}\"!");

            return false;
        }

        try
        {
            database.ApplyFile(temp, false);
        }
        catch (Exception)
        {
            _logger.Fatal($"Failed to create database {database.GetDatabaseName()}! Does the user (named in *.conf) have `CREATE`, `ALTER`, `DROP`, `INSERT` and `DELETE` privileges on the MySQL server?");
            File.Delete(temp);

            return false;
        }

        _logger.Information("Done.");
        File.Delete(temp);

        return true;
    }

    public bool Load()
    {
        if (_updateFlags == 0)
            _logger.Information("Automatic database updates are disabled for all databases!");

        if (_updateFlags != 0 && !DBExecutableUtil.CheckExecutable(_configuration))
            return false;

        if (!OpenDatabases())
            return false;

        if (!PopulateDatabases())
            return false;

        if (!UpdateDatabases())
            return false;

        if (!PrepareStatements())
            return false;

        return true;
    }

    private bool OpenDatabases()
    {
        return Process(_open);
    }

    private bool PopulateDatabases()
    {
        return Process(_populate);
    }

    private bool PrepareStatements()
    {
        return Process(_prepare);
    }

    // Processes the elements of the given stack until a predicate returned false.
    private bool Process(List<Func<bool>> list)
    {
        while (!list.Empty())
        {
            if (!list[0].Invoke())
                return false;

            list.RemoveAt(0);
        }

        return true;
    }

    private bool UpdateDatabases()
    {
        return Process(_update);
    }
}