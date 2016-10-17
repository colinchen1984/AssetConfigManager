using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AssetConfigManager
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class AssetConfigTarget : Attribute
	{
		public readonly Type TargetType = null;

		public AssetConfigTarget(Type targetType)
		{
			TargetType = targetType;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class InspectorShowingName : Attribute
	{
		public readonly string Name = null;

		public InspectorShowingName(string name)
		{
			Name = name;
		}
	}

	public abstract class AssetConfigApply
	{
		private static void SetValue(object source, FieldInfo sourceField, object target, FieldInfo targetField)
		{
			if (targetField.FieldType != sourceField.FieldType)
			{
				return;
			}

			if (false == targetField.FieldType.IsPrimitive && false == targetField.FieldType.IsEnum  && targetField.FieldType != typeof(string))
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetField.GetValue(target);
				SetAssetConfig(sourceValue, targetValue);
			}
			else
			{
				var value = sourceField.GetValue(source);
				targetField.SetValue(target, value);
			}
		}

		private static void SetValue(object source, FieldInfo sourceField, object target, PropertyInfo targetProperty)
		{
			if (targetProperty.PropertyType != sourceField.FieldType)
			{
				return;
			}
			if (false == targetProperty.PropertyType.IsPrimitive && false == targetProperty.PropertyType.IsEnum && targetProperty.PropertyType != typeof(string))
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetProperty.GetValue(target, new object[] { });
				SetAssetConfig(sourceValue, targetValue);
			}
			else
			{
				var value = sourceField.GetValue(source);
				targetProperty.SetValue(target, value, new object[] { });
			}
		}
		private static void SetAssetConfig(object source, object target)
		{
			var configType = source.GetType();
			var fields = configType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			var importType = target.GetType();
			foreach (var field in fields)
			{
				var targetProperty = importType.GetProperty(field.Name);
				if (null != targetProperty)
				{
					SetValue(source, field, target, targetProperty);
					continue;
				}
				var targetField = importType.GetField(field.Name);
				if (null != targetField)
				{
					SetValue(source, field, target, targetField);
					continue;
				}

			}
		}

		public virtual void SetAssetConfig(AssetImporter import)
		{
			SetAssetConfig(this, import);
		}
	}

	[Serializable]
	[AssetConfigTarget(typeof(TextureImporter))]
	public class TextureConfig : AssetConfigApply
	{
		public FilterMode filterMode = FilterMode.Bilinear;
		public TextureImporterFormat androidTextureFormat = TextureImporterFormat.AutomaticCompressed;
		public TextureImporterFormat iOSTextureFormat = TextureImporterFormat.AutomaticCompressed; public int compressionQuality = (int)TextureCompressionQuality.Normal;
		public TextureWrapMode wrapMode = TextureWrapMode.Repeat;

		[InspectorShowingName("Enable texture read/write")]
		public bool isReadable = false;
		public bool mipmapEnabled = false;
		public bool allowsAlphaSplit = true;
		public int maxTextureSize = 512;

		public override void SetAssetConfig(AssetImporter import)
		{
			base.SetAssetConfig(import);
			var ti = (TextureImporter) import;
			ti.SetPlatformTextureSettings("iPhone", maxTextureSize, iOSTextureFormat, compressionQuality, allowsAlphaSplit);
			ti.SetPlatformTextureSettings("Android", maxTextureSize, androidTextureFormat, compressionQuality, allowsAlphaSplit);
		}
	}

	[Serializable]
	[AssetConfigTarget(typeof(ModelImporter))]
	public class ModelConfig : AssetConfigApply
	{
		public ModelImporterAnimationType animationType = ModelImporterAnimationType.None;
		public ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Off;
		[InspectorShowingName("Enable Mesh read/write")]
		public bool isReadable = false;
		[InspectorShowingName("optimize Game Objects")]
		public bool optimizeGameObjects = true;
		public bool importAnimation = false;
		public bool importNormals = false;
		public bool importTangents = false;
	}

	[Serializable]
	[AssetConfigTarget(typeof(AudioImporter))]
	public class AudioConfig : AssetConfigApply
	{
		[InspectorShowingName("")]
		public AudioImporterSampleSettings defaultSampleSettings = default(AudioImporterSampleSettings);
		public bool forceToMono;
		public bool loadInBackground;
		public bool preloadAudioData = true;
	}

	[Serializable]
	public class AssetConfig : ScriptableObject
	{
		[InspectorShowingName("")]
		public AudioConfig AudioConfig;
		[InspectorShowingName("")]
		public ModelConfig ModelConfig;
		[InspectorShowingName("")]
		public TextureConfig TextureConfig;
	}
}