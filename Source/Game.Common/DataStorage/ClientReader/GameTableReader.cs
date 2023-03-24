// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using Framework.Collections;

namespace Game.Common.DataStorage.ClientReader;

public class GameTableReader
{
	internal static GameTable<T> Read<T>(string path, string fileName, ref uint loadedFileCount) where T : new()
	{
		GameTable<T> storage = new();

		if (!File.Exists(path + fileName))
		{
			Log.Logger.Error("File {0} not found.", fileName);

			return storage;
		}

		using (var reader = new StreamReader(path + fileName))
		{
			var headers = reader.ReadLine();

			if (headers.IsEmpty())
			{
				Log.Logger.Error("GameTable file {0} is empty.", fileName);

				return storage;
			}

			List<T> data = new();
			data.Add(new T()); // row id 0, unused

			string line;

			while (!(line = reader.ReadLine()).IsEmpty())
			{
				var values = new StringArray(line, '\t');

				if (values.IsEmpty())
					break;

				var obj = new T();
				var fields = obj.GetType().GetFields();

				for (int fieldIndex = 0, valueIndex = 1; fieldIndex < fields.Length && valueIndex < values.Length; ++fieldIndex, ++valueIndex)
				{
					var field = fields[fieldIndex];

					if (field.FieldType.IsArray)
					{
						var array = (Array)field.GetValue(obj);

						for (var i = 0; i < array.Length; ++i)
							array.SetValue(float.Parse(values[valueIndex++]), i);
					}
					else
					{
						fields[fieldIndex].SetValue(obj, float.Parse(values[valueIndex]));
					}
				}

				data.Add(obj);
			}

			storage.SetData(data);
		}

		loadedFileCount++;

		return storage;
	}
}
