using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetConfigManager
{
	[CustomEditor(typeof(AssetConfig))]
	public class AssetConfigInspector : Editor
	{
		private bool settingChanged = false;
		[MenuItem("Assets/Create Asset Auditing Rule")]
		public static void CreateConfig()
		{

			string selectionpath = "Assets";
			foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
			{
				selectionpath = AssetDatabase.GetAssetPath(obj);
				if (File.Exists(selectionpath))
				{
					selectionpath = Path.GetDirectoryName(selectionpath);
				}
				break;
			}
			string newRuleFileName = (Path.Combine(selectionpath, AssetImportPostProcessor.DefaultAssetConfigName));
			if (File.Exists(newRuleFileName))
			{
				EditorUtility.DisplayDialog("Config is there for you", "Can't have to configs in one folder", "OK", "Cancel");
				return;
			}
			var tree = AssetImportPostProcessor.BuildFolderTree();
			var parentConfig = tree.GetAssetConfig(selectionpath, true);
			AssetConfig newRule = null;
			if (null == parentConfig)
			{
				newRule = CreateInstance<AssetConfig>();
				newRuleFileName = newRuleFileName.Replace("\\", "/");
				AssetDatabase.CreateAsset(newRule, newRuleFileName);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			else
			{
				var parentPath = AssetDatabase.GetAssetPath(parentConfig);
				AssetDatabase.CopyAsset(parentPath, newRuleFileName);
				AssetDatabase.Refresh();
				newRule = AssetDatabase.LoadAssetAtPath<AssetConfig>(newRuleFileName);
			}
			Selection.activeObject = newRule;
			EditorUtility.FocusProjectWindow();
		}

		void OnDisable()
		{
			if (settingChanged)
			{
				EditorUtility.SetDirty(target);
				if (EditorUtility.DisplayDialog("Unsaved Settings", "Unsaved AssetRule Changes", "Apply", "Revert"))
				{
					ApplyAssetConfig((AssetConfig) target);

				}
				else
				{
					Undo.PerformUndo();
				}

			}
			settingChanged = false;
		}

		public override void OnInspectorGUI()
		{
			var t = (AssetConfig)target;
			EditorGUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			ObjectToInspector.ShowObjectInstance(t, "");
			settingChanged = settingChanged || EditorGUI.EndChangeCheck();
			if (settingChanged)
			{
				if (GUILayout.Button("Apply"))
				{
					ApplyAssetConfig(t);
					settingChanged = false;
				}
			}
			EditorGUILayout.EndVertical();
		}
	
		private void ApplyAssetConfig(AssetConfig config)
		{
			AssetImportPostProcessor.ApplyConfig(config);
			EditorUtility.SetDirty(config);
			AssetDatabase.SaveAssets();
		}
	}
}