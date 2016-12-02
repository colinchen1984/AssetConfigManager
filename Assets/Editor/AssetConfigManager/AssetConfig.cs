using System;
using System.Reflection;
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

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class ForceShowInInspector : Attribute
	{
		public ForceShowInInspector()
		{
		}
	}

	public abstract class AssetConfigApply
	{
		private static bool SetValue(object source, FieldInfo sourceField, object target, FieldInfo targetField)
		{
			var ret = false;
			if (targetField.FieldType != sourceField.FieldType)
			{
				return ret;
			}

			if (false == targetField.FieldType.IsPrimitive && false == targetField.FieldType.IsEnum && targetField.FieldType != typeof(string))
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetField.GetValue(target);
				ret = SetAssetConfig(sourceValue, targetValue);
			}
			else
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetField.GetValue(target);
				if(false == targetValue.Equals(sourceValue))
				{
					ret = true;
					targetField.SetValue(target, sourceValue);
				}
			}
			return true;
		}

		private static bool SetValue(object source, FieldInfo sourceField, object target, PropertyInfo targetProperty)
		{
			var ret = false;
			if (targetProperty.PropertyType != sourceField.FieldType)
			{
				return ret;
			}
			if (false == targetProperty.PropertyType.IsPrimitive && false == targetProperty.PropertyType.IsEnum && targetProperty.PropertyType != typeof(string))
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetProperty.GetValue(target, new object[] { });
				ret = SetAssetConfig(sourceValue, targetValue);
			}
			else
			{
				var sourceValue = sourceField.GetValue(source);
				var targetValue = targetProperty.GetValue(target, new object[] { });
				if(false == sourceValue.Equals(targetValue))
				{
					ret = true;
					targetProperty.SetValue(target, sourceValue, new object[] { });
				}
			}
			return ret;
		}

		private static bool SetAssetConfig(object source, object target)
		{
			var ret = false;
			var configType = source.GetType();
			var fields = configType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			var importType = target.GetType();
			foreach (var field in fields)
			{
				var targetProperty = importType.GetProperty(field.Name);
				if (null != targetProperty)
				{
					ret = SetValue(source, field, target, targetProperty) || ret;
					continue;
				}
				var targetField = importType.GetField(field.Name);
				if (null != targetField)
				{
					ret = SetValue(source, field, target, targetField) || ret;
					continue;
				}

			}
			return ret;
		}

		public virtual bool SetAssetConfig(AssetImporter import)
		{
			return SetAssetConfig(this, import);
		}
	}
	//http://blog.uwa4d.com/archives/LoadingPerformance_Texture.html
	//分辨率的影响
	//1、纹理资源的分辨率对加载性能影响较大，分辨率越高，其加载越为耗时。
	//	设备性能越差，其耗时差别越为明显；
	//2、设备越好，加载效率确实越高。但是，对于硬件支持纹理（ETC1/PVRTC）来说,
	//	中高端设备的加载效率差别已经很小，比如图中的红米Note2和三星S6设备，
	//	差别已经很不明显
	//纹理格式的影响
	//1、纹理资源的格式对加载性能影响同样较大，
	//	Android平台上，ETC1和ETC2的加载效率最高。
	//	同样，iOS平台上，PVRTC 4BPP的加载效率最高。
	//2、RGBA16格式纹理的加载效率同样很高，与RGBA32格式相比，
	//	其加载效率与ETC1/PVRTC非常接近，并且设备越好，加载开销差别越不明显；
	//3、RGBA32格式纹理的加载效率受硬件设备的性能影响较大，E
	//	TC/PVRTC/RGBA16受硬件设备的影响较低。
	//
	//总结
	//1、严格控制RGBA32和ARGB32纹理的使用，在保证视觉效果的前提下，
	//	尽可能采用“够用就好”的原则，降低纹理资源的分辨率，
	//	以及使用硬件支持的纹理格式。
	//2、在硬件格式（ETC、PVRTC）无法满足视觉效果时，RGBA16格式是一种较为理想的
	//	折中选择，既可以增加视觉效果，又可以保持较低的加载耗时。
	//3、严格检查纹理资源的Mipmap功能，特别注意UI纹理的Mipmap是否开启。
	//	在UWA测评过的项目中，有不少项目的UI纹理均开启了Mipmap功能，
	//	不仅造成了内存占用上的浪费，同时也增加了不小的加载时间。
	//4、ETC2对于支持OpenGL ES3.0的Android移动设备来说，是一个很好的处理半透明
	//	的纹理格式。但是，如果你的游戏需要在大量OpenGL ES2.0的设备上进行运行，
	//	那么我们不建议使用ETC2格式纹理。
	//	因为不仅会造成大量的内存占用（ETC2转成RGBA32），
	//	同时也增加一定的加载时间。
	//	下图为测试2中所用的测试纹理在三星S3和S4设备上加载性能表现。
	//	可以看出，在OpenGL ES2.0设备上，ETC2格式纹理的加载要明显高于ETC1格式，
	//	且略高于RGBA16格式纹理。因此，建议研发团队在项目中谨慎使用ETC2格式纹理。
	[Serializable]
	[AssetConfigTarget(typeof(TextureImporter))]
	public class TextureConfig : AssetConfigApply
	{
		public FilterMode filterMode = FilterMode.Bilinear;
		public TextureImporterFormat androidTextureFormat = TextureImporterFormat.AutomaticCompressed;
		public TextureImporterFormat iOSTextureFormat = TextureImporterFormat.AutomaticCompressed; public int compressionQuality = (int)TextureCompressionQuality.Normal;
		public TextureWrapMode wrapMode = TextureWrapMode.Repeat;

		[InspectorShowingName("Enable texture read/write")]
		[Tooltip("如果需要开启,请和程序确认,开启后导致纹理占用的内存翻倍")]
		public bool isReadable = false;
		public bool mipmapEnabled = false;
		public bool allowsAlphaSplit = true;
		public int maxTextureSize = 512;

		public override bool SetAssetConfig(AssetImporter import)
		{
			var ret = base.SetAssetConfig(import);
			var ti = (TextureImporter)import;
			int maxTextureSizeSource = 0;
			TextureImporterFormat formartSource = TextureImporterFormat.Alpha8;
			ti.GetPlatformTextureSettings("iPhone", out maxTextureSizeSource, out formartSource);
			ret = ret || maxTextureSizeSource != maxTextureSize || formartSource != iOSTextureFormat;
			ti.GetPlatformTextureSettings("Android", out maxTextureSizeSource, out formartSource);
			ret = ret || maxTextureSizeSource != maxTextureSize || formartSource != androidTextureFormat;
			ti.SetPlatformTextureSettings("iPhone", maxTextureSize, iOSTextureFormat, compressionQuality, allowsAlphaSplit);
			ti.SetPlatformTextureSettings("Android", maxTextureSize, androidTextureFormat, compressionQuality, allowsAlphaSplit);
			return ret;
		}
	}

	//http://blog.uwa4d.com/archives/LoadingPerformance_Mesh.html
	//1、在保证视觉效果的前提下，尽可能采用“够用就好”的原则，即降低网格资源的顶点数量和面片数量；
	//2、研发团队对于顶点属性的使用需谨慎处理。通过以上分析可以看出，顶点属性越多，则内存占用越高，加载时间越长；
	//3、如果在项目运行过程中对网格资源数据不进行读写操作（比如Morphing动画等），那么建议将Read/Write功能关闭，既可以提升加载效率，又可以大幅度降低内存占用。
	//PS: 如果是用于粒子系统的mesh,需要开启read/write,不然会导致崩溃
	[Serializable]
	[AssetConfigTarget(typeof(ModelImporter))]
	public class ModelConfig : AssetConfigApply
	{

		[InspectorShowingName("Enable Mesh read/write")]
		[Tooltip("用于例子系统的Mesh需要开启,其他需要开启,请和程序确认,开启后导致Mesh占用的内存翻倍")]
		public bool isReadable = false;
		[InspectorShowingName("optimize Game Objects")]
		[Tooltip("用于新的MacAnimation系统,Unity会自动计算并删除不需要的骨骼,开启后,对于挂载点,需要额外处理")]
		public bool optimizeGameObjects = true;
		public bool importAnimation = false;
		public ModelImporterAnimationType animationType = ModelImporterAnimationType.None;
		public ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Off;
		[Tooltip("建议开启,不然会导致打包的时候,引入默认的材质")]
		public bool importMaterials = false;
		public ModelImporterMaterialName materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
		public ModelImporterNormals importNormals = ModelImporterNormals.None;
		public ModelImporterTangents importTangents = ModelImporterTangents.None;
		public ModelImporterAnimationCompression AnimationCompression = ModelImporterAnimationCompression.KeyframeReduction;
	}

	[Serializable]
	[AssetConfigTarget(typeof(AudioImporter))]
	public class AudioConfig : AssetConfigApply
	{
		[ForceShowInInspector]
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