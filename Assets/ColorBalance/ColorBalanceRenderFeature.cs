using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorBalanceRenderFeature : ScriptableRendererFeature
{
   // public RenderPassEvent Event=RenderPassEvent.AfterRenderingOpaques;
    class ColorBalancePass : ScriptableRenderPass
    {
        private Material colorBalanceMaterial;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;
        private ColorBalance colorBalance;
        private TestVolume01 testVolume01;
        public ColorBalancePass(Material material)
        {
            colorBalanceMaterial = material;
            tempTexture.Init("_TemporaryColorTexture");
        }

        public void Setup(ColorBalance colorBalance, TestVolume01 testVolume)
        {
            this.colorBalance = colorBalance;
            this.testVolume01 = testVolume;
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
            // 合并判断逻辑，允许任意一个 VolumeComponent 激活
            bool isColorBalanceActive = colorBalance != null && colorBalance.IsActive();
            bool isTestVolumeActive = testVolume01 != null && testVolume01.IsActive();
            if (!isColorBalanceActive && !isTestVolumeActive) return;

            CommandBuffer cmd = CommandBufferPool.Get("ColorBalance");
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc);

            // 先设置材质参数
            if (isColorBalanceActive)
            {
                colorBalanceMaterial.SetColor("_Shadows", colorBalance.shadows.value);
                colorBalanceMaterial.SetColor("_Midtones", colorBalance.midtones.value);
                colorBalanceMaterial.SetColor("_Highlights", colorBalance.highlights.value);
            }

            if (isTestVolumeActive)
            {
                colorBalanceMaterial.SetFloat("_Saturation", testVolume01._Saturation.value);
                colorBalanceMaterial.SetFloat("_Contrast", testVolume01._Contrast.value);
            }

            // 再执行 Blit
            Blit(cmd, source, tempTexture.Identifier(), colorBalanceMaterial, 0);
            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }
    
    
    
    
    class BloomPass : ScriptableRenderPass
    {
        private Material material;
        private BloomSettings bloomSettings;
        private RenderTargetHandle bloomTexture;

        public BloomPass(Material mat)
        {
            material = mat;
            bloomTexture.Init("_BloomTempTex");
        }
        public void Setup(BloomSettings settings)
        {
            bloomSettings = settings;
        }
        
        // public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        // { // 渲染前回调
        //     //  RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        //     //   blitTargetDescriptor.depthBufferBits = 0;
        //     ScriptableRenderer renderer = renderingData.cameraData.renderer;
        //     bloomTexture = renderer.cameraColorTarget;
        //     //  cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
        //     //  destination = new RenderTargetIdentifier(destinationId);
        // }
        
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool isBloomActive = bloomSettings != null && bloomSettings.IsActive();
            if (!isBloomActive ) return;
            CommandBuffer cmd = CommandBufferPool.Get("Bloom");

            // 读取 ColorBalance 的结果
            var source = Shader.PropertyToID("_ColorBalanceResult");
            cmd.GetTemporaryRT(bloomTexture.id, renderingData.cameraData.cameraTargetDescriptor);

            // 应用 Bloom 处理（高光提取、模糊等）
            Blit(cmd, source, bloomTexture.Identifier(), material, 0);

            // 将 Bloom 结果叠加到最终画面
            Blit(cmd, bloomTexture.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(bloomTexture.id);
        }
    }
    
    
    
    
    

    public Material colorBalanceMaterial;
    private ColorBalancePass colorBalancePass;
    private BloomPass bloomPass;

    public override void Create()
    {
        //colorBalancePass = new ColorBalancePass(colorBalanceMaterial);
    
       // colorBalancePass.renderPassEvent = Event;
       colorBalancePass = new ColorBalancePass(colorBalanceMaterial)
       {
           renderPassEvent = RenderPassEvent.AfterRenderingOpaques
       };

       bloomPass = new BloomPass(colorBalanceMaterial)
       {
           renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
       };

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var volumeStack = VolumeManager.instance.stack;
        var colorBalance = volumeStack.GetComponent<ColorBalance>();
        var testVolume = volumeStack.GetComponent<TestVolume01>();

        // 允许任意一个 VolumeComponent 激活时执行
        if ((colorBalance != null && colorBalance.IsActive()) || (testVolume != null && testVolume.IsActive()))
        {
            colorBalancePass.Setup(colorBalance, testVolume);
            renderer.EnqueuePass(colorBalancePass);
        }
        var bloomSettings = volumeStack.GetComponent<BloomSettings>();
        if (bloomSettings != null && bloomSettings.IsActive())
        {
            bloomPass.Setup(bloomSettings);
            renderer.EnqueuePass(bloomPass);
        }
    }
}
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
//
// public class ColorBalanceRenderFeature : ScriptableRendererFeature
// {
//     class ColorBalancePass : ScriptableRenderPass
//     {
//         private Material colorBalanceMaterial;
//         private RenderTargetHandle tempTexture;
//         private ColorBalance colorBalance;
//
//         public ColorBalancePass(Material material)
//         {
//             colorBalanceMaterial = material;
//             tempTexture.Init("_TemporaryColorTexture");
//         }
//
//         public void Setup(ColorBalance colorBalance)
//         {
//             this.colorBalance = colorBalance;
//         }
//
//         public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//         {
//             if (colorBalance == null || !colorBalance.IsActive())
//                 return;
//
//             CommandBuffer cmd = CommandBufferPool.Get("ColorBalance");
//             RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
//
//             cmd.GetTemporaryRT(tempTexture.id, opaqueDesc);
//             Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, tempTexture.Identifier(), colorBalanceMaterial, 0);
//
//             colorBalanceMaterial.SetColor("_Shadows", colorBalance.shadows.value);
//             colorBalanceMaterial.SetColor("_Midtones", colorBalance.midtones.value);
//             colorBalanceMaterial.SetColor("_Highlights", colorBalance.highlights.value);
//
//             Blit(cmd, tempTexture.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
//
//             context.ExecuteCommandBuffer(cmd);
//             CommandBufferPool.Release(cmd);
//         }
//
//         public override void FrameCleanup(CommandBuffer cmd)
//         {
//             cmd.ReleaseTemporaryRT(tempTexture.id);
//         }
//     }
//
//     public Material colorBalanceMaterial;
//     private ColorBalancePass colorBalancePass;
//
//     public override void Create()
//     {
//         colorBalancePass = new ColorBalancePass(colorBalanceMaterial)
//         {
//             renderPassEvent = RenderPassEvent.AfterRenderingTransparents
//         };
//     }
//
//     public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//     {
//         var volumeStack = VolumeManager.instance.stack;
//         var colorBalance = volumeStack.GetComponent<ColorBalance>();
//
//         if (colorBalance != null && colorBalance.IsActive())
//         {
//             colorBalancePass.Setup(colorBalance);
//             renderer.EnqueuePass(colorBalancePass);
//         }
//     }
// }
