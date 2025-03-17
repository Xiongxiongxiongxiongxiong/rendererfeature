using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/BloomSettings")]
public class BloomSettings : VolumeComponent, IPostProcessComponent
{
    [Header("Bloom Settings")]
    public FloatParameter threshold = new FloatParameter(1.0f);    // 高光提取阈值
    public FloatParameter intensity = new FloatParameter(1.0f);    // 泛光强度
    public ColorParameter bloomColor = new ColorParameter(Color.white); // 泛光颜色
    public ClampedFloatParameter blurSize = new ClampedFloatParameter(1.0f, 0.1f, 10.0f); // 模糊半径

    public bool IsActive() => intensity.value > 0;
    public bool IsTileCompatible() => false;
}