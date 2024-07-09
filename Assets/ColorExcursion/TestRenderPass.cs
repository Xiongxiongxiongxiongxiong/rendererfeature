using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestRenderPass : ScriptableRenderPass
{
    private ScriptableRenderer currentTarget;
    private TestVolume volume;
    public TestRenderPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }
    public void Setup(ScriptableRenderer currentTarget)
    {
        this.currentTarget = currentTarget;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled) return;

        var stack = VolumeManager.instance.stack;
        volume = stack.GetComponent<TestVolume>();
        if (volume == null) return;
        if (!volume.IsActive()) return;

        var cmd = CommandBufferPool.Get("TestRenderPass");

        var source = currentTarget.cameraColorTargetHandle;
        int temTextureID = Shader.PropertyToID("_TestTex");
        cmd.GetTemporaryRT(temTextureID, source.rt.descriptor);

      //  var material = volume.material.value;
       var s = Shader.Find("Unlit/Test");
       var material = new Material(s);
        material.SetFloat("_Offs", volume.offset.value);
        cmd.Blit(source, temTextureID, material, 0);
        cmd.Blit(temTextureID, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
} 
