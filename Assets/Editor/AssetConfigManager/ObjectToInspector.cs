using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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

		private static string GetShowingTip(FieldInfo fieldInfo)
		{
			var tip = string.Empty;
			var nameAttr = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false);
			if (nameAttr.Length > 0)
			{
				tip = ((TooltipAttribute)nameAttr[0]).tooltip;
			}
			return tip;
		}

		private static void ShowError(FieldInfo fieldInfo, object targetObject)
		{
			new Exception(string.Format("Wrong field type {0}\t{1}", fieldInfo.FieldType.Name, fieldInfo.Name));
		}

		private static void ShowBoolValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);
			var tip = GetShowingTip(fieldInfo);
			var content = new GUIContent(name, tip);
			bool value = GetValue<bool>(fieldInfo, targetObject);
			var newValue = EditorGUILayout.Toggle(content, value);
			if (newValue != value)
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowIntValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);
			var tip = GetShowingTip(fieldInfo);
			var content = new GUIContent(name, tip);
			EditorGUILayout.LabelField(content);

			var value = GetValue<int>(fieldInfo, targetObject);
			var newValue = EditorGUILayout.IntField(value);
			if (newValue != value)
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowFloatValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);
			var tip = GetShowingTip(fieldInfo);
			var content = new GUIContent(name, tip);
			EditorGUILayout.LabelField(content);

			var value = GetValue<float>(fieldInfo, targetObject);
			var newValue = EditorGUILayout.FloatField(value);
			if (false == newValue.Equals(value))
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		private static void ShowEnumValue(FieldInfo fieldInfo, object targetObject)
		{
			var name = GetShowingName(fieldInfo);
			var tip = GetShowingTip(fieldInfo);
			var content = new GUIContent(name, tip);
			EditorGUILayout.LabelField(content);
			var value = GetValue<Enum>(fieldInfo, targetObject);
			var newValue = EditorGUILayout.EnumPopup(value);
			if (false == newValue.Equals(value))
			{
				fieldInfo.SetValue(targetObject, newValue);
			}
		}

		public static void ShowObjectInstance(FieldInfo objFieldInfo, object targetObject, string objectName)
		{
			var serilizeable = targetObject.GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			var allFields = targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			var finalFields = new List<FieldInfo>(allFields.Length);
			if (serilizeable.Length == 0)
			{
				if (null != objFieldInfo)
				{
					serilizeable = objFieldInfo.GetCustomAttributes(typeof(ForceShowInInspector), false);
					if (serilizeable.Length > 0)
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
				}
				else
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
					var value = field.GetValue(targetObject);
					ShowObjectInstance(field, value, GetShowingName(field));
					field.SetValue(targetObject, value);
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