using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class BloomSettings
    {
        public Material bloomMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Range(0.0f, 1.0f)] public float threshold = 0.8f;
        [Range(0.0f, 8.0f)] public float radius = 4.0f;
        [Range(1, 8)] public int iterations = 4;
        [Range(0.0f, 2.0f)] public float intensity = 1.0f;
    }

    class BloomPass : ScriptableRenderPass
    {
        private Material material;
        private BloomSettings settings;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTex1;
        private RenderTargetHandle tempTex2;

        public BloomPass(BloomSettings settings)
        {
            this.settings = settings;
            this.material = settings.bloomMaterial;
            tempTex1.Init("_TempBloomTex1");
            tempTex2.Init("_TempBloomTex2");
        }

        // 删除Setup方法，改为在Execute中获取source
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempTex1.id, cameraTextureDescriptor);
            cmd.GetTemporaryRT(tempTex2.id, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Bloom Effect");
            
            material.SetFloat("_BloomThreshold", settings.threshold);
            material.SetFloat("_BloomIntensity", settings.intensity);
            material.SetFloat("_BloomRadius", settings.radius);

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width /= 2;
            desc.height /= 2;

            // 设置纹理参数
            cmd.SetGlobalVector("_MainTex_TexelSize", new Vector4(1.0f / desc.width, 1.0f / desc.height, desc.width, desc.height));

            // 1. 亮区提取
            cmd.Blit(source, tempTex1.Identifier(), material, 0);

            // 2. 模糊迭代
            for (int i = 0; i < settings.iterations; i++)
            {
                // 水平模糊
                cmd.Blit(tempTex1.Identifier(), tempTex2.Identifier(), material, 1);
                // 垂直模糊
                cmd.Blit(tempTex2.Identifier(), tempTex1.Identifier(), material, 2);
            }

            // 3. 合成
            cmd.SetGlobalTexture("_BloomTex", tempTex1.Identifier());
            cmd.Blit(source, tempTex2.Identifier(), material, 3);
            cmd.Blit(tempTex2.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTex1.id);
            cmd.ReleaseTemporaryRT(tempTex2.id);
        }
    }

    public BloomSettings settings = new BloomSettings();
    private BloomPass bloomPass;

    public override void Create()
    {
        bloomPass = new BloomPass(settings);
        bloomPass.renderPassEvent = settings.renderPassEvent;
    }

    // 修改后的AddRenderPasses方法
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.bloomMaterial == null) return;
        renderer.EnqueuePass(bloomPass);
    }
}