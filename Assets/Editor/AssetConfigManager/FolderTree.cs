using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetConfigManager
{
	public class Folder
	{
		public string Path { get; private set; }

		public readonly Folder Parent;

		public AssetConfig Config { get; private set; }

		public readonly IList<Folder> Children = new List<Folder>(16);

		public Folder(string name, string path, Folder parent, AssetConfig config)
		{
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

		public void SetPath(string path)
		{
			Path = path;
		}
	}

	public class FolderTree 
	{

		private readonly Func<string, AssetConfig> configLoader = delegate { return null; };
		private readonly Action<AssetConfig> configDestorier = delegate {};

		private Dictionary<string, Folder> folderDic = new Dictionary<string, Folder>(1024);

		private readonly string rootPath = string.Empty;

		private readonly string assetConfigDefaultName = string.Empty;

		private Folder LoadFolder(DirectoryInfo folderInfo, string configFileName, Folder parent, string relatedPath)
		{
			var folderPath = folderInfo.FullName; 
			relatedPath = Path.Combine(relatedPath, folderInfo.Name);
			var configPath = Path.Combine(relatedPath, configFileName);
			var config = configLoader(configPath);
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

		private void DeleteFolder(Folder folder)
		{
			foreach (var child in folder.Children)
			{
				DeleteFolder(child);
			}
			if (null != folder.Config)
			{
				configDestorier(folder.Config);
			}
			folderDic.Remove(folder.Path);
		}

		public void AddFolderInfo(string path)
		{
			var dicInfo = new DirectoryInfo(path);
			var parent = dicInfo.Parent;
			Debug.Assert(parent != null);
			var parentFolderInfo = GetFolderInfo(parent.FullName);
			Debug.Assert(null != parentFolderInfo);
			var folderInfo = new Folder(path, path, parentFolderInfo, null);
			parentFolderInfo.AddChildFolder(folderInfo);
			folderDic.Add(dicInfo.FullName, folderInfo);
		}

		public bool AddAssetConfig(string path)
		{
			var folder = GetFolderInfo(Path.GetDirectoryName(path));
			if (null == folder)
			{
				return false;
			}

			var config = configLoader(path);
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

		public void DeleteAssetConfig(string path)
		{
			var f = GetFolderInfo(Path.GetDirectoryName(path));
			if (null != f.Config)
			{
				configDestorier(f.Config);
				f.SetAssetConfig(null);
			}
		}

		public void MoveFolderInfo(string source, string destination)
		{
			var f = GetFolderInfo(source);
			if (null == f)
			{
				return;
			}

			if (null != f.Parent)
			{
				f.Parent.RemoveChildFolder(f);
			}

			f.SetPath(destination);
			var dicInfo = new DirectoryInfo(destination);
			var parent = GetFolderInfo(dicInfo.Parent.FullName);
			parent.AddChildFolder(f);
		}

		public void MoveAssetConfig(string source, string destination)
		{
			var sourceFolder = GetFolderInfo(Path.GetDirectoryName(source));
			var desFolder = GetFolderInfo(Path.GetDirectoryName(destination));
			desFolder.SetAssetConfig(sourceFolder.Config);
			sourceFolder.SetAssetConfig(null);
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

		public FolderTree(string rootPath, string assetConfigDefaultName, Func<string, AssetConfig> configLoader, Action<AssetConfig> configDestorier)
		{
			Debug.Log("");
			this.rootPath = rootPath;
			this.assetConfigDefaultName = assetConfigDefaultName;
			this.configLoader = configLoader;
			this.configDestorier = configDestorier;
		}

		public void Init()
		{
			folderDic.Clear();
			var directoryInfo = new DirectoryInfo(rootPath);
			LoadFolder(directoryInfo, assetConfigDefaultName, null, "");
		}
	}
}