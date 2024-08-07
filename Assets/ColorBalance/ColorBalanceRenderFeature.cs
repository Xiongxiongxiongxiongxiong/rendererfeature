using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorBalanceRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent Event=RenderPassEvent.AfterRenderingOpaques;
    class ColorBalancePass : ScriptableRenderPass
    {
        private Material colorBalanceMaterial;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;
        private ColorBalance colorBalance;

        public ColorBalancePass(Material material)
        {
            colorBalanceMaterial = material;
            tempTexture.Init("_TemporaryColorTexture");
        }

        public void Setup( ColorBalance colorBalance)
        {
          //  source = src;
            this.colorBalance = colorBalance;
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
            if (colorBalance == null || !colorBalance.IsActive())
                return;

            CommandBuffer cmd = CommandBufferPool.Get("ColorBalance");
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc);
            Blit(cmd, source, tempTexture.Identifier(), colorBalanceMaterial, 0);

            colorBalanceMaterial.SetColor("_Shadows", colorBalance.shadows.value);
            colorBalanceMaterial.SetColor("_Midtones", colorBalance.midtones.value);
            colorBalanceMaterial.SetColor("_Highlights", colorBalance.highlights.value);

            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }

    public Material colorBalanceMaterial;
    private ColorBalancePass colorBalancePass;

    public override void Create()
    {
        colorBalancePass = new ColorBalancePass(colorBalanceMaterial);
    
        colorBalancePass.renderPassEvent = Event;

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var volumeStack = VolumeManager.instance.stack;
        var colorBalance = volumeStack.GetComponent<ColorBalance>();

        if (colorBalance != null && colorBalance.IsActive())
        {
            colorBalancePass.Setup( colorBalance);
            renderer.EnqueuePass(colorBalancePass);
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
