using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorAdjustments : VolumeComponent, IPostProcessComponent
{

  //  public MaterialParameter material = new MaterialParameter(null);
  //public Shader shader;
    //public FloatParameter offset = new FloatParameter(0.1f);
    public FloatParameter 饱和度 = new FloatParameter(1f);
    public FloatParameter 对比度 = new FloatParameter(1f);
    public bool IsActive()
    {
        // _Saturation.value != 1f;
        // _Contrast.value != 1f;
        return true;//material.value != null;
    }

    public bool IsTileCompatible()
    {
        return false;
    }

} 
