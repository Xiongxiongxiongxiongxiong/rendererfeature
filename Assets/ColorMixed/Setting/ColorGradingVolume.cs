using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("自定义后效/线性伽马增益")]
public class ColorGradingVolume : VolumeComponent
{

    // 参数暴露在 Volume 面板中
    public ColorParameter 线性 = new ColorParameter(Color.black, false, false, true);
    public ColorParameter 伽马 = new ColorParameter(Color.white, false, false, true);
    public ColorParameter 增益 = new ColorParameter(Color.white, false, false, true);
}