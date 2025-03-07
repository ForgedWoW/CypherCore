﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Framework.Configuration;

namespace Framework.Database;

public class DatabaseUpdater<T>
{
	readonly MySqlBase<T> _database;

	public DatabaseUpdater(MySqlBase<T> database)
	{
		_database = database;
	}

	public bool Populate()
	{
		var result = _database.Query("SHOW TABLES");

		if (!result.IsEmpty() && !result.IsEmpty())
			return true;

		Log.outInfo(LogFilter.SqlUpdates, $"Database {_database.GetDatabaseName()} is empty, auto populating it...");

		var path = GetSourceDirectory();
		var fileName = "Unknown";

		switch (_database.GetType().Name)
		{
			case "LoginDatabase":
				fileName = @"/sql/base/auth_database.sql";

				break;
			case "CharacterDatabase":
				fileName = @"/sql/base/characters_database.sql";

				break;
			case "WorldDatabase":
				fileName = @"/sql/TDB_full_world_1005.23021_2023_02_03.sql";

				break;
			case "HotfixDatabase":
				fileName = @"/sql/TDB_full_hotfixes_1005.23021_2023_02_03.sql";

				break;
		}

		if (!File.Exists(path + fileName))
		{
			Log.outError(LogFilter.SqlUpdates,
						$"File \"{path + fileName}\" is missing, download it from \"https://github.com/TrinityCore/TrinityCore/releases\"" +
						" and place it in your sql directory.");

			return false;
		}

		// Update database
		Log.outInfo(LogFilter.SqlUpdates, $"Applying \'{fileName}\'...");

		try
		{
			ApplyFile(path + fileName);
		}
		catch (Exception)
		{
			return false;
		}

		Log.outInfo(LogFilter.SqlUpdates, $"Done Applying \'{fileName}\'");

		return true;
	}

	public bool Update()
	{
		Log.outInfo(LogFilter.SqlUpdates, $"Updating {_database.GetDatabaseName()} database...");

		var sourceDirectory = GetSourceDirectory();

		if (!Directory.Exists(sourceDirectory))
		{
			Log.outError(LogFilter.SqlUpdates, $"DBUpdater: The given source directory {sourceDirectory} does not exist, change the path to the directory where your sql directory exists (for example c:\\source\\cyphercore). Shutting down.");

			return false;
		}

		var availableFiles = GetFileList();
		var appliedFiles = ReceiveAppliedFiles();

		var redundancyChecks = ConfigMgr.GetDefaultValue("Updates.Redundancy", true);
		var archivedRedundancy = ConfigMgr.GetDefaultValue("Updates.Redundancy", true);

		UpdateResult result = new();

		// Count updates
		foreach (var entry in appliedFiles)
			if (entry.Value.State == State.RELEASED)
				++result.recent;
			else
				++result.archived;

		foreach (var availableQuery in availableFiles)
		{
			Log.outDebug(LogFilter.SqlUpdates, $"Checking update \"{availableQuery.GetFileName()}\"...");

			var applied = appliedFiles.LookupByKey(availableQuery.GetFileName());

			if (applied != null)
			{
				// If redundancy is disabled skip it since the update is already applied.
				if (!redundancyChecks)
				{
					Log.outDebug(LogFilter.SqlUpdates, "Update is already applied, skipping redundancy checks.");
					appliedFiles.Remove(availableQuery.GetFileName());

					continue;
				}

				// If the update is in an archived directory and is marked as archived in our database skip redundancy checks (archived updates never change).
				if (!archivedRedundancy && (applied.State == State.ARCHIVED) && (availableQuery.state == State.ARCHIVED))
				{
					Log.outDebug(LogFilter.SqlUpdates, "Update is archived and marked as archived in database, skipping redundancy checks.");
					appliedFiles.Remove(availableQuery.GetFileName());

					continue;
				}
			}

			// Calculate hash
			var hash = CalculateHash(availableQuery.path);

			var mode = UpdateMode.Apply;

			// Update is not in our applied list
			if (applied == null)
			{
				// Catch renames (different filename but same hash)
				var hashIter = appliedFiles.Values.FirstOrDefault(p => p.Hash == hash);

				if (hashIter != null)
				{
					// Check if the original file was removed if not we've got a problem.
					var renameFile = availableFiles.Find(p => p.GetFileName() == hashIter.Name);

					if (renameFile != null)
					{
						Log.outWarn(LogFilter.SqlUpdates,
									$"Seems like update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\' was renamed, but the old file is still there! " +
									$"Trade it as a new file! (Probably its an unmodified copy of file \"{renameFile.GetFileName()}\")");
					}
					// Its save to trade the file as renamed here
					else
					{
						Log.outInfo(LogFilter.SqlUpdates, $"Renaming update \"{hashIter.Name}\" to \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'.");

						RenameEntry(hashIter.Name, availableQuery.GetFileName());
						appliedFiles.Remove(hashIter.Name);

						continue;
					}
				}
				// Apply the update if it was never seen before.
				else
				{
					Log.outInfo(LogFilter.SqlUpdates, $"Applying update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'...");
				}
			}
			// Rehash the update entry if it is contained in our database but with an empty hash.
			else if (ConfigMgr.GetDefaultValue("Updates.AllowRehash", true) && string.IsNullOrEmpty(applied.Hash))
			{
				mode = UpdateMode.Rehash;

				Log.outInfo(LogFilter.SqlUpdates, $"Re-hashing update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'...");
			}
			else
			{
				// If the hash of the files differs from the one stored in our database reapply the update (because it was changed).
				if (applied.Hash != hash && applied.State != State.ARCHIVED)
				{
					Log.outInfo(LogFilter.SqlUpdates, $"Reapplying update \"{availableQuery.GetFileName()}\" \'{applied.Hash.Substring(0, 7)}\' . \'{hash.Substring(0, 7)}\' (it changed)...");
				}
				else
				{
					// If the file wasn't changed and just moved update its state if necessary.
					if (applied.State != availableQuery.state)
					{
						Log.outDebug(LogFilter.SqlUpdates, $"Updating state of \"{availableQuery.GetFileName()}\" to \'{availableQuery.state}\'...");

						UpdateState(availableQuery.GetFileName(), availableQuery.state);
					}

					Log.outDebug(LogFilter.SqlUpdates, $"Update is already applied and is matching hash \'{hash.Substring(0, 7)}\'.");

					appliedFiles.Remove(applied.Name);

					continue;
				}
			}

			uint speed = 0;
			AppliedFileEntry file = new(availableQuery.GetFileName(), hash, availableQuery.state, 0);

			switch (mode)
			{
				case UpdateMode.Apply:
					speed = ApplyTimedFile(availableQuery.path);
					goto case UpdateMode.Rehash;
				case UpdateMode.Rehash:
					UpdateEntry(file, speed);

					break;
			}

			if (applied != null)
				appliedFiles.Remove(applied.Name);

			if (mode == UpdateMode.Apply)
				++result.updated;
		}

		// Cleanup up orphaned entries if enabled
		if (!appliedFiles.Empty())
		{
			var cleanDeadReferencesMaxCount = ConfigMgr.GetDefaultValue("Updates.CleanDeadRefMaxCount", 3);
			var doCleanup = (cleanDeadReferencesMaxCount < 0) || (appliedFiles.Count <= cleanDeadReferencesMaxCount);

			foreach (var entry in appliedFiles)
			{
				Log.outTrace(LogFilter.SqlUpdates, $"File \'{entry.Key}\' was applied to the database but is missing in your update directory now!");

				if (doCleanup)
					Log.outInfo(LogFilter.SqlUpdates, $"Deleting orphaned entry \'{entry.Key}\'...");
			}

			if (doCleanup)
				CleanUp(appliedFiles);
			else
				Log.outError(LogFilter.SqlUpdates, $"Cleanup is disabled! There are {appliedFiles.Count} dirty files that were applied to your database but are now missing in your source directory!");
		}

		var info = $"Containing {result.recent} new and {result.archived} archived updates.";

		if (result.updated == 0)
			Log.outInfo(LogFilter.SqlUpdates, $"{_database.GetDatabaseName()} database is up-to-date! {info}");
		else
			Log.outInfo(LogFilter.SqlUpdates, $"Applied {result.updated} query(s). {info}");

		return true;
	}

	string GetSourceDirectory()
	{
		return ConfigMgr.GetDefaultValue("Updates.SourcePath", "../../../");
	}

	uint ApplyTimedFile(string path)
	{
		// Benchmark query speed
		var oldMSTime = Time.MSTime;

		// Update database
		ApplyFile(path);

		// Return time the query took to apply
		return Time.GetMSTimeDiffToNow(oldMSTime);
	}

	void ApplyFile(string path)
	{
		_database.ApplyFile(path);
	}

	void Apply(string query)
	{
		_database.Execute(query);
	}

	void UpdateEntry(AppliedFileEntry entry, uint speed)
	{
		var update = $"REPLACE INTO `updates` (`name`, `hash`, `state`, `speed`) VALUES (\"{entry.Name}\", \"{entry.Hash}\", \'{entry.State}\', {speed})";

		// Update database
		Apply(update);
	}

	void RenameEntry(string from, string to)
	{
		// Delete target if it exists
		{
			var update = $"DELETE FROM `updates` WHERE `name`=\"{to}\"";

			// Update database
			Apply(update);
		}

		// Rename
		{
			var update = $"UPDATE `updates` SET `name`=\"{to}\" WHERE `name`=\"{from}\"";

			// Update database
			Apply(update);
		}
	}

	void CleanUp(Dictionary<string, AppliedFileEntry> storage)
	{
		if (storage.Empty())
			return;

		var remaining = storage.Count;
		var update = "DELETE FROM `updates` WHERE `name` IN(";

		foreach (var entry in storage)
		{
			update += $"\"{entry.Key}\"";

			if ((--remaining) > 0)
				update += ", ";
		}

		update += ")";

		// Update database
		Apply(update);
	}

	void UpdateState(string name, State state)
	{
		var update = $"UPDATE `updates` SET `state`=\'{state}\' WHERE `name`=\"{name}\"";

		// Update database
		Apply(update);
	}

	List<FileEntry> GetFileList()
	{
		List<FileEntry> fileList = new();

		var result = _database.Query("SELECT `path`, `state` FROM `updates_include`");

		if (result.IsEmpty())
			return fileList;

		do
		{
			var path = result.Read<string>(0);

			if (path[0] == '$')
				path = GetSourceDirectory() + path.Substring(1);

			if (!Directory.Exists(path))
			{
				Log.outTrace(LogFilter.SqlUpdates, $"DBUpdater: Given update include directory \"{path}\" isn't existing, skipped!");

				continue;
			}

			var state = result.Read<string>(1).ToEnum<State>();

			foreach (var file in GetFilesFromDirectory(path, state))
				fileList.Add(file);

			Log.outDebug(LogFilter.SqlUpdates, $"Added applied file \"{path}\" from remote.");
		} while (result.NextRow());


		var moreFiles = ConfigMgr.GetDefaultValue($"Updates.{_database.GetType().Name}.Path", "");

		if (!string.IsNullOrEmpty(moreFiles) && Directory.Exists(moreFiles))
			foreach (var file in GetFilesFromDirectory(moreFiles, State.RELEASED))
				fileList.Add(file);

		return fileList;
	}

	Dictionary<string, AppliedFileEntry> ReceiveAppliedFiles()
	{
		Dictionary<string, AppliedFileEntry> map = new();

		var result = _database.Query("SELECT `name`, `hash`, `state`, UNIX_TIMESTAMP(`timestamp`) FROM `updates` ORDER BY `name` ASC");

		if (result.IsEmpty())
			return map;

		do
		{
			AppliedFileEntry entry = new(result.Read<string>(0), result.Read<string>(1), result.Read<string>(2).ToEnum<State>(), result.Read<ulong>(3));
			map.Add(entry.Name, entry);
		} while (result.NextRow());

		return map;
	}

	IEnumerable<FileEntry> GetFilesFromDirectory(string directory, State state)
	{
		Queue<string> queue = new();
		queue.Enqueue(directory);

		while (queue.Count > 0)
		{
			directory = queue.Dequeue();

			try
			{
				foreach (var subDir in Directory.GetDirectories(directory).OrderBy(p => p))
					queue.Enqueue(subDir);
			}
			catch (Exception ex)
			{
				Log.outFatal(LogFilter.SqlUpdates, $"DBUpdater: {directory} Exception: {ex}");
			}

			var files = Directory.GetFiles(directory, "*.sql").OrderBy(p => p).ToList();

			for (var i = 0; i < files.Count; i++)
				yield return new FileEntry(files[i], state);
		}
	}

	string CalculateHash(string fileName)
	{
		using (var sha1 = SHA1.Create())
		{
			var text = File.ReadAllText(fileName).Replace("\r", "");

			return sha1.ComputeHash(Encoding.UTF8.GetBytes(text)).ToHexString();
		}
	}
}