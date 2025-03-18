using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("自定义后效/色彩平衡")]
//[Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Bloom", typeof(UniversalRenderPipeline))]
public class ColorBalance : VolumeComponent, IPostProcessComponent
{
    public ColorParameter 暗部 = new ColorParameter(Color.white, false, false, true);
    public ColorParameter 灰部 = new ColorParameter(Color.white, false, false, true);
    public ColorParameter 亮部 = new ColorParameter(Color.white, false, false, true);
    // public float _Saturation;
    // public float _Contrast;
    // public FloatParameter _Saturation = new FloatParameter(1f);
    // public FloatParameter _Contrast = new FloatParameter(1f);
    public bool IsActive() => 暗部.value != Color.white || 灰部.value != Color.white || 亮部.value != Color.white;
    public bool IsTileCompatible() => false;
}