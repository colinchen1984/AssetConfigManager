using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AssetConfigManager
{
	public class Folder
	{
		public readonly string Name;

		public readonly string Path;

		public readonly Folder Parent;

		public AssetConfig Config { get; private set; }

		public readonly IList<Folder> Children = new List<Folder>(16);

		public Folder(string name, string path, Folder parent, AssetConfig config)
		{
			Name = name;
			Path = path;
			Parent = parent;
			Config = config;
		}

		public void AddChildFolder(Folder child)
		{
			Children.Add(child);
		}

		public void RemoveChildFolder(Folder child)
		{
			Children.Remove(child);
		}

		public void SetAssetConfig(AssetConfig config)
		{
			Config = config;
		}
	}

	public class FolderTree
	{

		public Func<string, AssetConfig> ConfigLoader = delegate(string s) { return null; };

		private Dictionary<string, Folder> folderDic = new Dictionary<string, Folder>(1024);

		private Folder root = null;

		private Folder LoadFolder(DirectoryInfo folderInfo, string configFileName, Folder parent, string relatedPath)
		{
			var folderPath = folderInfo.FullName;
			relatedPath = Path.Combine(relatedPath, folderInfo.Name);
			var configPath = Path.Combine(relatedPath, configFileName);
			var config = ConfigLoader(configPath);
			var folder = new Folder(folderInfo.Name, folderPath, parent, config);
			if (null != parent)
			{
				parent.AddChildFolder(folder);
			}
			folderDic.Add(folderPath, folder);
			var kids = folderInfo.GetDirectories();
			foreach (var child in kids)
			{
				LoadFolder(child, configFileName, folder, relatedPath);
			}
			return folder;
		}

		private void DeleteFolder(Folder folder)
		{
			foreach (var child in folder.Children)
			{
				DeleteFolder(child);
			}

			folderDic.Remove(folder.Path);
		}

		public FolderTree(string rootPath, string configFileName)
		{
			ConfigLoader = s =>
			{
				var config = AssetDatabase.LoadAssetAtPath<AssetConfig>(s);
				return config;
			};
			Init(rootPath, configFileName);
		}
		public void Init(string rootPath, string configFileName)
		{
			folderDic.Clear();
			var directoryInfo = new DirectoryInfo(rootPath);
			root = LoadFolder(directoryInfo, configFileName, null, "");
		}

		public Folder GetFolderInfo(string path)
		{
			Folder ret = null;
			do
			{
				if (false == Directory.Exists(path))
				{
					break;
				}

				var absolutePath = Path.GetFullPath(path);

				folderDic.TryGetValue(absolutePath, out ret);

			} while (false);
			return ret;
		}

		public bool DeleteFolderInfo(string path)
		{
			bool ret = false;
			do
			{
				Folder folder = GetFolderInfo(path);
				if (null == folder)
				{
					break;
				}

				DeleteFolder(folder);
				if (null != folder.Parent)
				{
					folder.Parent.RemoveChildFolder(folder);
				}

				ret = true;
			} while (false);
			return ret;
		}

		public bool AddAssetFolder(string path, AssetConfig config)
		{
			var folder = GetFolderInfo(path);
			if (null == folder)
			{
				return false;
			}

			folder.SetAssetConfig(config);
			return true;
		}

		public AssetConfig GetAssetConfig(string path, bool hirachy)
		{
			var folder = GetFolderInfo(path);
			if (null == folder)
			{
				return null;
			}

			if (false == hirachy)
			{
				return folder.Config;
			}

			AssetConfig ret = null;
			while (ret == null && folder != null)
			{
				ret = folder.Config;
				folder = folder.Parent;
			}
			return ret;
		}

	}
}