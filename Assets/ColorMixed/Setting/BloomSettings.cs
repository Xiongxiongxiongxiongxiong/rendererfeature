using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("自定义后效/辉光")]
public class BloomSettings : VolumeComponent, IPostProcessComponent
{
    [Header("辉光参数")]
    public FloatParameter 辉光阈值 = new FloatParameter(0.0f);    // 高光提取阈值
    public FloatParameter 辉光强度 = new FloatParameter(0.0f);    // 泛光强度
    public ColorParameter 辉光颜色 = new ColorParameter(Color.white); // 泛光颜色
    public FloatParameter 辉光半径 = new FloatParameter(0.0f);      
    public FloatParameter 辉光迭代 = new FloatParameter(0.0f); 
    public bool IsActive() => 辉光强度.value > 0;
    public bool IsTileCompatible() => false;
}