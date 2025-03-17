using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

public class FullscreenFeature : ScriptableRendererFeature
{
  //  public Settings settings = new Settings(); // 设置
    FullscreenPass blitPass; // 后处理的Pass
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    public Material blitMaterial = null;
    public float Saturation;
    public float Contrast;



    public class FullscreenPass : ScriptableRenderPass
    {
        // public FullscreenFeature settings; // 配置项

        private Material bMaterial ;
        private string profilerTag; // 分析器标签, 在Frame Debugger中可以看到该标签
        private RenderTargetIdentifier source; // 源缓存标识
        private RenderTargetIdentifier destination; // 目标缓存标识
        private int destinationId; // 目标缓存id
        private FilterMode filterMode; // 纹理采样滤波模式, 取值有: Point、Bilinear、Trilinear
        public float _Contrast;
        public float _Saturation;

        public FullscreenPass(string tag , Material material,float Contrast ,float Saturation)
        {
            profilerTag = tag;
            bMaterial = material; // 将传入的材质赋值给bMaterial
            _Contrast = Contrast;
            _Saturation = Saturation;
            destinationId = Shader.PropertyToID("_TempRT");
        }

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
        { // 执行渲染
            bMaterial.SetFloat("_Saturation", _Saturation);
            bMaterial.SetFloat("_Contrast", _Contrast);
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            Blit(cmd, source, destination, bMaterial);
            Blit(cmd, destination, source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        { // 渲染后回调
            if (destinationId != -1)
            {
                cmd.ReleaseTemporaryRT(destinationId);
            }
        }
    }









    public override void Create()
    { // 创建后处理Pass(自动回调)

        blitPass = new FullscreenPass(name, blitMaterial , Contrast, Saturation);
      //  blitPass._Contrast = Contrast;
      //  blitPass._Saturation = Saturation;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    { // 添加渲染Pass(自动回调)
        if (blitMaterial == null)
        {
            return;
        }
        blitPass.renderPassEvent = renderPassEvent;
        // blitPass.settings = new FullscreenFeature();
       // blitPass
        renderer.EnqueuePass(blitPass);
    }

   // [System.Serializable]
    //public class Settings
    //{ // 配置项
    //    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    //    public Material blitMaterial = null;
   // }
}