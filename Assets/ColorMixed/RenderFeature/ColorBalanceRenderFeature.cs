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
        private ColorAdjustments ColorAdjustments;
        private ColorGradingVolume colorGradingVolume;
        public ColorBalancePass(Material material)
        {
            colorBalanceMaterial = material;
            tempTexture.Init("_TemporaryColorTexture");
        }

        public void Setup(ColorBalance colorBalance, ColorAdjustments testVolume,ColorGradingVolume colorGrading)
        {
            this.colorBalance = colorBalance;
            this.ColorAdjustments = testVolume;
            this.colorGradingVolume = colorGrading;
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
            bool isTestVolumeActive = ColorAdjustments != null && ColorAdjustments.IsActive();
            bool iscolorGradingVolume=colorGradingVolume !=null ;
            if (!isColorBalanceActive && !isTestVolumeActive) return;

            CommandBuffer cmd = CommandBufferPool.Get("ColorBalance");
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc);

            // 先设置材质参数
            if (isColorBalanceActive)
            {
                colorBalanceMaterial.SetColor("_Shadows", colorBalance.暗部.value);
                colorBalanceMaterial.SetColor("_Midtones", colorBalance.灰部.value);
                colorBalanceMaterial.SetColor("_Highlights", colorBalance.亮部.value);
            }

            if (isTestVolumeActive)
            {
                colorBalanceMaterial.SetFloat("_Saturation", ColorAdjustments.饱和度.value);
                colorBalanceMaterial.SetFloat("_Contrast", ColorAdjustments.对比度.value);
            }
            if (iscolorGradingVolume)
            {
                colorBalanceMaterial.SetColor("_Lift", colorGradingVolume.线性.value);
                colorBalanceMaterial.SetColor("_Gamma", colorGradingVolume.伽马.value);
                colorBalanceMaterial.SetColor("_Gain", colorGradingVolume.增益.value);
            }
            // 再执行 Blit
            Blit(cmd, source, tempTexture.Identifier(), colorBalanceMaterial, 0);
            Blit(cmd, tempTexture.Identifier(), source,colorBalanceMaterial, 0);

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
        private RenderTargetIdentifier source; // 添加源目标标识符
        private RenderTargetHandle bloomTexture;
        private RenderTargetHandle tempTex1;
        private RenderTargetHandle tempTex2;

        public BloomPass(Material mat)
        {
            material = mat;
            bloomTexture.Init("_BloomTempTex");
            tempTex1.Init("_TempBloomTex1");
            tempTex2.Init("_TempBloomTex2");
        }
        public void Setup(BloomSettings settings)
        {
            bloomSettings = settings;
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
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempTex1.id, cameraTextureDescriptor);
            cmd.GetTemporaryRT(tempTex2.id, cameraTextureDescriptor);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool isBloomActive = bloomSettings != null && bloomSettings.IsActive();
            if (!isBloomActive ) return;
            CommandBuffer cmd = CommandBufferPool.Get("Bloom");

            
            
            material.SetFloat("_BloomThreshold", bloomSettings.辉光阈值.value);
            material.SetFloat("_BloomIntensity", bloomSettings.辉光强度.value);
            material.SetFloat("_BloomRadius", bloomSettings.辉光半径.value);
           //material.SetFloat("_bloomColor", bloomSettings.bloomColor.value);
           material.SetColor("_bloomColor", bloomSettings.辉光颜色.value);
            
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / 2);
            desc.height = Mathf.Max(1, desc.height / 2);
            desc.depthBufferBits = 0;
            
            // 申请下采样的临时纹理
            cmd.GetTemporaryRT(tempTex1.id, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(tempTex2.id, desc, FilterMode.Bilinear);
            
            // 设置纹理参数
            //cmd.SetGlobalVector("_MainTex_TexelSize", new Vector4(1.0f / desc.width, 1.0f / desc.height, desc.width, desc.height));

            // 1. 亮区提取
            cmd.Blit(source, tempTex1.Identifier(), material, 0);

            // 2. 模糊迭代
            for (int i = 0; i < bloomSettings.辉光迭代.value; i++)
            {
                // 水平模糊
                cmd.Blit(tempTex1.Identifier(), tempTex2.Identifier(), material, 1);
                // 垂直模糊
                cmd.Blit(tempTex2.Identifier(), tempTex1.Identifier(), material, 2);
            }
            
            
            // 3. 合成
            RenderTextureDescriptor finalDesc = renderingData.cameraData.cameraTargetDescriptor;
            finalDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTex2.id, finalDesc, FilterMode.Bilinear); // 重用tempTex2作为
            
            cmd.SetGlobalTexture("_BloomTex", tempTex1.Identifier());
            cmd.Blit(source, tempTex2.Identifier(), material, 3);
            cmd.Blit(tempTex2.Identifier(), source);

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
        var testVolume = volumeStack.GetComponent<ColorAdjustments>();
        var colorGradingVolume = volumeStack.GetComponent<ColorGradingVolume>();
        // 允许任意一个 VolumeComponent 激活时执行
        if ((colorBalance != null && colorBalance.IsActive()) || (testVolume != null && testVolume.IsActive()) ||colorGradingVolume !=null)
        {
            colorBalancePass.Setup(colorBalance, testVolume,colorGradingVolume);
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
