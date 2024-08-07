using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("Custom/ColorBalance")]
public class ColorBalance : VolumeComponent, IPostProcessComponent
{
    public ColorParameter shadows = new ColorParameter(Color.white, false, false, true);
    public ColorParameter midtones = new ColorParameter(Color.white, false, false, true);
    public ColorParameter highlights = new ColorParameter(Color.white, false, false, true);

    public bool IsActive() => shadows.value != Color.white || midtones.value != Color.white || highlights.value != Color.white;
    public bool IsTileCompatible() => false;
}