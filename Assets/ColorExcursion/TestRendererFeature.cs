using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestRendererFeature : ScriptableRendererFeature
{
    TestRenderPass testRenderPass;
    public override void Create()
    {
        testRenderPass = new TestRenderPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        testRenderPass.Setup(renderer);
        renderer.EnqueuePass(testRenderPass);
    }

} 


