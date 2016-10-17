using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace AssetConfigManager
{
	public static class ObjectToInspector
	{
		private static Dictionary<Type, Action<FieldInfo, object>> showerDic = new Dictionary<Type, Action<FieldInfo, object>>()
		{
			{typeof(bool), ShowBoolValue},
			{typeof(int), ShowIntValue},
			{typeof(float), ShowFloatValue},
			{typeof(Enum), ShowEnumValue},
		};

		private static T GetValue<T>(FieldInfo fieldInfo, object targetObject)
		{
			return (T) fieldInfo.GetValue(targetObject);
		}

		private static string GetShowingName(FieldInfo fieldInfo)
		{
			var name = fieldInfo.Name;
			var nameAttr = fieldInfo.GetCustomAttributes(typeof(InspectorShowingName), false);
			if (nameAttr.Length > 0)
			{
				name = ((InspectorShowingName)nameAttr[0]).Name;
			}
			return name;
		}

		private static void ShowError(FieldInfo fieldInfo, object targetObject)
		{
			new Exception(string.Format("Wrong field type {0}\t{1}", fieldInfo.FieldType.Name, fieldInfo.Name));
		}

		private static void ShowBoolValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);
			bool value = GetValue<bool>(fieldInfo, targetObject);
			var newValue = EditorGUILayout.Toggle(name, value);
			if (newValue != value)
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowIntValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);

			var value = GetValue<int>(fieldInfo, targetObject);
			EditorGUILayout.LabelField(name);
			var newValue = EditorGUILayout.IntField(value);
			if (newValue != value)
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowFloatValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);

			var value = GetValue<float>(fieldInfo, targetObject);
			EditorGUILayout.LabelField(name);
			var newValue = EditorGUILayout.FloatField(value);
			if (false == newValue.Equals(value))
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowEnumValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);

			var value = GetValue<Enum>(fieldInfo, targetObject);
			EditorGUILayout.LabelField(name);
			var newValue = EditorGUILayout.EnumPopup(value);
			if (false == newValue.Equals(value))
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		public static void ShowObjectInstance(object targetObject, string objectName)
		{
			var serilizeable = targetObject.GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			var allFields = targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			var finalFields = new List<FieldInfo>(allFields.Length);
			if (serilizeable.Length == 0)
			{
				foreach (var fieldInfo in allFields)
				{
					var a = fieldInfo.GetCustomAttributes(typeof(SerializableAttribute), false);
					if (a.Length > 0)
					{
						finalFields.Add(fieldInfo);
					}
				}
			}
			else
			{
				foreach (var fieldInfo in allFields)
				{
					var a = fieldInfo.GetCustomAttributes(typeof(NonSerializedAttribute), false);
					if (a.Length == 0)
					{
						finalFields.Add(fieldInfo);
					}
				}
			}
			EditorGUILayout.BeginHorizontal();
			var strLabel = string.Format("{0}\t{1}",
				targetObject.GetType().Name, objectName);
			EditorGUILayout.LabelField(strLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical();
			foreach (var field in finalFields)
			{
				if (false == field.FieldType.IsPrimitive && false == field.FieldType.IsEnum)
				{
					var newTarget = field.GetValue(targetObject);
					ShowObjectInstance(newTarget, GetShowingName(field));
				}
				else
				{
					Action<FieldInfo, object> shower = ShowError;
					shower = showerDic.TryGetValue(field.FieldType, out shower) ? shower : ShowError;
					if (field.FieldType.IsEnum)
					{
						shower = ShowEnumValue;
					}
					EditorGUILayout.BeginHorizontal();
					shower(field, targetObject);
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndVertical();
		}
	}
}