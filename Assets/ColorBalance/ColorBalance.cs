using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("Custom/ColorBalance")]
//[Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Bloom", typeof(UniversalRenderPipeline))]
public class ColorBalance : VolumeComponent, IPostProcessComponent
{
    public ColorParameter shadows = new ColorParameter(Color.white, false, false, true);
    public ColorParameter midtones = new ColorParameter(Color.white, false, false, true);
    public ColorParameter highlights = new ColorParameter(Color.white, false, false, true);
    // public float _Saturation;
    // public float _Contrast;
    public FloatParameter _Saturation = new FloatParameter(1f);
    public FloatParameter _Contrast = new FloatParameter(1f);
    public bool IsActive() => shadows.value != Color.white || midtones.value != Color.white || highlights.value != Color.white||_Saturation.value !=1f||_Contrast.value !=1f;
    public bool IsTileCompatible() => false;
}