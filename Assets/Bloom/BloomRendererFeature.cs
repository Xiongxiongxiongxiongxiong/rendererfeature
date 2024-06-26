using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class BloomSettings
    {
        public Material bloomMaterial = null;
    }

    public BloomSettings settings = new BloomSettings();
    BloomRenderPass bloomPass;

    class BloomRenderPass : ScriptableRenderPass
    {
        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle tempTexture;
        private Material bloomMaterial;
        private int destinationId; // 目标缓存id
        private FilterMode filterMode; // 纹理采样滤波模式, 取值有: Point、Bilinear、Trilinear
        private RenderTargetIdentifier destination; // 目标缓存标识

        public BloomRenderPass(Material material)
        {
            this.bloomMaterial = material;
            tempTexture.Init("_TempBloomTexture");
        }

        //public void Setup(RenderTargetIdentifier source)
        //{
        //    this.source = source;
        //}
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { // 渲染前回调
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
            cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
            destination = new RenderTargetIdentifier(destinationId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (bloomMaterial == null)
            {
                Debug.LogError("Bloom Material is null");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Bloom Pass");

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTexture.id, descriptor, FilterMode.Bilinear);

            Blit(cmd, source, tempTexture.Identifier(), bloomMaterial, 0);
            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (tempTexture != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(tempTexture.id);
            }
        }
    }



    public override void Create()
    {
        bloomPass = new BloomRenderPass(settings.bloomMaterial);
        //{
        //    renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        //};
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.bloomMaterial == null)
        {
            Debug.LogWarning("Missing Bloom Material");
            return;
        }
        bloomPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    //   bloomPass.Setup(renderer.cameraColorTarget);
    renderer.EnqueuePass(bloomPass);
    }
}
