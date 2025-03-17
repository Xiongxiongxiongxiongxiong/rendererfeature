using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/Color Grading")]
public class ColorGradingVolume : VolumeComponent
{
    // 参数暴露在 Volume 面板中
    public ColorParameter lift = new ColorParameter(Color.white, false, false, true);
    public ColorParameter gamma = new ColorParameter(Color.white, false, false, true);
    public ColorParameter gain = new ColorParameter(Color.white, false, false, true);
}