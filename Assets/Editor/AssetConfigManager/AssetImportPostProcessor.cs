using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AssetConfigManager
{
	[InitializeOnLoad]
	public class AssetImportPostProcessor : UnityEditor.AssetModificationProcessor
	{
		public static readonly string DefaultAssetConfigName = "AssetConfig.asset";
		private static readonly string Root = "Assets";
		private static readonly string AssetTypes = "t:texture2D t:Model t:AudioClip";

		public static FolderTree tree = null;

		static AssetImportPostProcessor()
		{
			tree = new FolderTree(Root, DefaultAssetConfigName, (string path) =>
			{
				path = path.Replace("\\", "/");
				var ret = AssetDatabase.LoadAssetAtPath<AssetConfig>(path); 
				return ret;
			}, (config => Object.DestroyImmediate(config, true)));
			tree.Init();
		}

		// Define the event handlers.
		private static string[] OnWillSaveAssets(string[] list)
		{

			var configs = (from s in list
						   where s.Contains(DefaultAssetConfigName)
						   select s).ToArray();


			if (configs.Length > 0)
			{
				EditorApplication.delayCall += () =>
				{
					foreach (var configPath in configs)
					{
						var config = tree.GetAssetConfig(Path.GetDirectoryName(configPath), false);
						ApplyConfigToAllFileUnderConfig(configPath, config);
					}
				};
			}
			return list;
		}

		private static void OnWillCreateAsset(string path)
		{
			var extention = Path.GetExtension(path);
			if (false == extention.Equals(".meta"))
			{
				return;
			}
			path = path.Replace(extention, "");
			EditorApplication.delayCall += () =>
			{
				if (Directory.Exists(path))
				{
					tree.AddFolderInfo(path);
				}
				else if (path.Contains(DefaultAssetConfigName))
				{
					tree.AddAssetConfig(path);
					ApplyConfigToAllFileUnderConfig(path, GetConfigByAssetName(path));
				}
				else
				{
					ImportAsset(path);
				}

			};
		}

		private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions op)
		{
			if (Directory.Exists(path))
			{
				tree.DeleteFolderInfo(path);
			}
			else if (path.Contains(DefaultAssetConfigName))
			{
				tree.DeleteAssetConfig(path);
			}
			return AssetDeleteResult.DidNotDelete;
		}

		private static AssetMoveResult OnWillMoveAsset(string source, string destination)
		{
			if (source.Contains(DefaultAssetConfigName))
			{
				if (false == destination.Contains(DefaultAssetConfigName))
				{
					return AssetMoveResult.FailedMove;
				}
			}

			if (Directory.Exists(source))
			{
				tree.MoveFolderInfo(source, destination);
			}
			else if (source.Contains(DefaultAssetConfigName))
			{
				tree.MoveAssetConfig(source, destination);
				ApplyConfigToAllFileUnderConfig(destination, GetConfigByAssetName(destination));
			}
			else
			{
				EditorApplication.delayCall += () =>
				{
					ImportAsset(destination);
				};
			}
			return AssetMoveResult.DidNotMove;
		}

		private static AssetConfig GetConfigByAssetName(string assetPath)
		{
			var config = tree.GetAssetConfig(Path.GetDirectoryName(assetPath), true);
			return config;
		}

		private static bool ImportAsset(AssetImporter import, AssetConfig config)
		{
			var ret = false;
			var importType = import.GetType();
			var fields = config.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (var field in fields)
			{
				var targetConfigType = field.FieldType.GetCustomAttributes(typeof(AssetConfigTarget), true);
				if (targetConfigType.Length == 0)
				{
					continue;
				}
				var targetType = (AssetConfigTarget) targetConfigType[0];
				if (targetType.TargetType != importType)
				{
					continue;
				}
				var configField = (AssetConfigApply)field.GetValue(config);
				configField.SetAssetConfig(import);
				ret = true;
				break;
			}
			return ret;
		}

		private static void ImportAsset(string assetPath, AssetConfig config)
		{
			var i = AssetImporter.GetAtPath(assetPath);
			if (ImportAsset(i, config))
			{
				i.SaveAndReimport();
			}
		}

		private static void ImportAsset(string assetPath)
		{
			var config = GetConfigByAssetName(assetPath);
			ImportAsset(assetPath, config);
		}

		public static void ApplyConfigToAllFileUnderConfig(string configPath, AssetConfig config)
		{
			var dicPath = Path.GetDirectoryName(configPath);
			var guidList = AssetDatabase.FindAssets(AssetTypes, new[] { dicPath });
			var filePath = (from file in guidList select AssetDatabase.GUIDToAssetPath(file));
			var all = filePath.All(f =>
			{
				var c = GetConfigByAssetName(f);
				if (object.ReferenceEquals(config, c))
				{
					ImportAsset(f, config);
				}
				return true;
			});
		}
	}
}