using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorGradingRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class ColorGradingSettings
    {
        public Material material; // 指向 ColorGradingMat
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    } 
    private class ColorGradingPass : ScriptableRenderPass
    {
        private Material _material;
        private ColorGradingVolume _volume;
        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle tempTexture;
        
        private int destinationId; // 目标缓存id
        private FilterMode filterMode; // 纹理采样滤波模式, 取值有: Point、Bilinear、Trilinear
        private RenderTargetIdentifier destination; // 目标缓存标识
        public ColorGradingPass(ColorGradingSettings settings)
        {
            renderPassEvent = settings.renderPassEvent;
            _material = settings.material;
            tempTexture.Init("_TempColorGradingTexture");
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { // 渲染前回调
            //  RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            //   blitTargetDescriptor.depthBufferBits = 0;
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
            //  cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
            //  destination = new RenderTargetIdentifier(destinationId);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || !renderingData.cameraData.postProcessEnabled) return;

            // 从 Volume 中获取参数
            var stack = VolumeManager.instance.stack;
            _volume = stack.GetComponent<ColorGradingVolume>();
            if (_volume == null) return;

            // 设置 Shader 参数
            _material.SetColor("_Lift", _volume.线性.value);
            _material.SetColor("_Gamma", _volume.伽马.value);
            _material.SetColor("_Gain", _volume.增益.value);

            // 获取相机的 RenderTexture
            CommandBuffer cmd = CommandBufferPool.Get("ColorGradingPass");
            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            // 应用材质
            //Blit(cmd, source, source, _material);
            descriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTexture.id, descriptor, FilterMode.Bilinear);

            Blit(cmd, source, tempTexture.Identifier(), _material, 0);
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
    public ColorGradingSettings settings = new ColorGradingSettings();
    private ColorGradingPass _colorGradingPass;

    public override void Create()
    {
        _colorGradingPass = new ColorGradingPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material != null)
        {
            renderer.EnqueuePass(_colorGradingPass);
        }
        _colorGradingPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        //   bloomPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_colorGradingPass);
    }


}