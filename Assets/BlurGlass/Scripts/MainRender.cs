using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MainRender : MonoBehaviour
{
    public RenderTexture _rt;
    void Start()
    {
        
    }
    private void OnEnable()
    {
       
    }
    private void OnDisable()
    {
        _rt.Release();
    }

    void Update()
    {
        
    }
}
