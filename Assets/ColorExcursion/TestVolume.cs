using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestVolume : VolumeComponent, IPostProcessComponent
{

  //  public MaterialParameter material = new MaterialParameter(null);
  //public Shader shader;
    public FloatParameter offset = new FloatParameter(0.1f);

    public bool IsActive()
    {
        return true;//material.value != null;
    }

    public bool IsTileCompatible()
    {
        return false;
    }

} 
