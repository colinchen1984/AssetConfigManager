using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AssetConfigManager
{
	public class AssetImportPostProcessor
	{
		public static readonly string DefaultAssetConfigName = "AssetConfig.asset";
		public static FolderTree tree = new FolderTree("Assets", DefaultAssetConfigName);

		public static FolderTree BuildFolderTree()
		{
			tree.ConfigLoader += s =>
			{
				var config = AssetDatabase.LoadAssetAtPath<AssetConfig>(s);
				return config;
			};
			tree.Init("Assets", DefaultAssetConfigName);
			return tree;
		}

		private static string GetFilePath(AssetImporter import)
		{
			var path = import.assetPath;
			path = path.Replace(Path.GetFileName(path), "");
			path = path.TrimEnd('/');
			return path;
		}

		private static AssetConfig GetConfig(AssetImporter import)
		{
			var folderPath = GetFilePath(import);
			var config = tree.GetAssetConfig(folderPath, true);
			return config;
		}

		private static bool ImportAsset(AssetImporter import, AssetConfig config)
		{
			var importType = import.GetType();
			var importConfig = GetConfig(import);
			if (null == importConfig || importConfig != config)
			{
				return false;
			}
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
				break;
			}
			return true;
		}

		public static void ApplyConfig(AssetConfig config)
		{
			BuildFolderTree();
			var path = AssetDatabase.GetAssetPath(config); 
			path = path.Replace(Path.GetFileName(path), "").TrimEnd('/');
			var guidList = AssetDatabase.FindAssets("t:texture2D t:Model t:AudioClip", new[] {path});
			var filePath = (from file in guidList select AssetDatabase.GUIDToAssetPath(file));
			var all = filePath.All(f =>
			{
				var i = AssetImporter.GetAtPath(f);
				if (ImportAsset(i, config))
				{
					i.SaveAndReimport();
				}
				return true;
			});
		}
	}
}