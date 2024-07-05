using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ZoomBlurRenderFeature : ScriptableRendererFeature
{
    ZoomBlurPass zoomBlurPass;
    
    class ZoomBlurPass : ScriptableRenderPass
    {
        private RenderTargetIdentifier source { get; set; }
         static readonly string k_RenderTag = "Render ZoomBlur Effects";
         private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
         private static readonly int TempTargetId = Shader.PropertyToID("_TempTargetZoomBlur");
         private static readonly int FocusPowerId = Shader.PropertyToID("_FocusPower");
         private static readonly int FocusDetailId = Shader.PropertyToID("_FocusDetail");
         private static readonly int FocusScreenPositionId = Shader.PropertyToID("_FocusScreenPosition");
         private static readonly int ReferenceResolutionXId = Shader.PropertyToID("_ReferenceResolutionX");

         private ZoomBlur zoomBlur;
         private Material zoomBlurMaterial;
         
         RenderTargetIdentifier currentTarget;
         
         
         //构造函数
         public ZoomBlurPass(RenderPassEvent evt)
         {
             renderPassEvent = evt;
             var shader = Shader.Find("PostEffect/ZoomBlur");
             if (shader ==null)
             {
                 Debug.LogError("shader不存在");
                 return;
             }

             zoomBlurMaterial = CoreUtils.CreateEngineMaterial(shader);
         }

         public void Setup(in RenderTargetIdentifier currentTar)
         {
             this.currentTarget = currentTar;
         }
         
         

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (zoomBlurMaterial == null) 
            {
                Debug.LogError("材质球不存在");
                return;
            }

            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            var stack = VolumeManager.instance.stack;
            zoomBlur = stack.GetComponent<ZoomBlur>();

            if (zoomBlur == null)
            {
                return;
            }

            if (!zoomBlur.IsActive())
            {
                return;
            }

            var cmd = CommandBufferPool.Get(k_RenderTag);

            Render(cmd, ref renderingData);
            
            
            
           
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            
        }

        void Render(CommandBuffer cmd ,ref RenderingData renderingData)
        {
            ref var camerData = ref renderingData.cameraData;
            var source = currentTarget;
            int destination = TempTargetId;
            var w = camerData.camera.scaledPixelWidth;
            var h = camerData.camera.scaledPixelHeight;
            //设置
            zoomBlurMaterial.SetFloat(FocusPowerId,zoomBlur.focusPower.value);
            zoomBlurMaterial.SetInt(FocusDetailId,zoomBlur.focusDetail.value);
            zoomBlurMaterial.SetVector(FocusScreenPositionId,zoomBlur.focusScreenPosition.value);
            zoomBlurMaterial.SetInt(ReferenceResolutionXId,zoomBlur.referrnceResolutionX.value);

            int shaderPass = 0;
            cmd.SetGlobalTexture(MainTexId,source);
            cmd.GetTemporaryRT(destination,w,h,0,FilterMode.Point,RenderTextureFormat.Default);
            cmd.Blit(source,destination);
            cmd.Blit(destination,source,zoomBlurMaterial,shaderPass);


 


        }
        

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }




    public override void Create()
    {
        zoomBlurPass = new ZoomBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);

       // zoomBlurPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
       // zoomBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(zoomBlurPass);
    }
}








