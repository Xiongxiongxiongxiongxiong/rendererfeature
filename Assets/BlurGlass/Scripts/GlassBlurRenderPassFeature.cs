using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlassBlurRenderPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public LayerMask layerMask = -1;
        public Material blurMat;
        public string textureName = "";
        public string cmdName = "";
        public string passName = "";
        public RenderTexture blurRt = null;
    }

    GlassBlurRenderPass m_ScriptablePass;
    private RenderTargetHandle dest;
    public Settings settings;







    public class GlassBlurRenderPass : ScriptableRenderPass
    {
        private CommandBuffer cmd;
        private string cmdName;
      //  private RenderTargetHandle dest;
        private Material m_blurMat;
        private RenderTexture m_blurRt;
        private RenderTargetIdentifier source { get; set; }
        RenderTargetHandle m_temporaryColorTexture;
        RenderTargetHandle blurredID;
        RenderTargetHandle blurredID2;

        


        private RenderTargetIdentifier destination; // 目标缓存标识
        private int destinationId; // 目标缓存id
        private FilterMode filterMode = FilterMode.Bilinear; // 纹理采样滤波模式, 取值有: Point、Bilinear、Trilinear



        public GlassBlurRenderPass(GlassBlurRenderPassFeature.Settings param)
        {
            renderPassEvent = param.renderEvent;
            cmdName = param.cmdName;
            m_blurMat = param.blurMat;
            m_blurRt = param.blurRt;

            blurredID.Init("blurredID");
            blurredID2.Init("blurredID2");
            destinationId = Shader.PropertyToID("_DestinationTexture");
        }



        //public void Setup(RenderTargetIdentifier src, RenderTargetHandle _dest)
        //{
        //    this.source = src;
        //    this.dest = _dest;
        //}

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { // 渲染前回调
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
          //  dest = RenderTargetHandle.CameraTarget;
            cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
            destination = new RenderTargetIdentifier(destinationId);
            
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

          //var src = renderer.cameraColorTarget;


            if (renderingData.cameraData.isSceneViewCamera) return;
            //如果cullingMask包含UI层的camera，返回
            if ((renderingData.cameraData.camera.cullingMask & 1 << LayerMask.NameToLayer("UI")) > 0)
                return;
            if (!GlassCtrl.takeShot)
                return;

            cmd = CommandBufferPool.Get(cmdName);
            Vector2[] sizes = {
                new Vector2(Screen.width, Screen.height),
                new Vector2(Screen.width / 2, Screen.height / 2),
                new Vector2(Screen.width / 4, Screen.height / 4),
                new Vector2(Screen.width / 8, Screen.height / 8),
            };

            int numIterations = 3;
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            opaqueDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_temporaryColorTexture.id, opaqueDesc, FilterMode.Bilinear);
            cmd.Blit(source, m_temporaryColorTexture.Identifier());
            for (int i = 0; i < numIterations; ++i)
            {

                cmd.GetTemporaryRT(blurredID.id, opaqueDesc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(blurredID2.id, opaqueDesc, FilterMode.Bilinear);

                cmd.Blit(m_temporaryColorTexture.Identifier(), blurredID.Identifier());
                cmd.SetGlobalVector("offsets", new Vector4(2.0f / sizes[i].x, 0, 0, 0));
                cmd.Blit(blurredID.Identifier(), blurredID2.Identifier(), m_blurMat);
                cmd.SetGlobalVector("offsets", new Vector4(0, 2.0f / sizes[i].y, 0, 0));
                cmd.Blit(blurredID2.Identifier(), blurredID.Identifier(), m_blurMat);

                cmd.Blit(blurredID.Identifier(), m_temporaryColorTexture.Identifier());
            }

            cmd.Blit(blurredID.Identifier(), m_blurRt);
            GlassCtrl.takeShot = false;

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            //Debug.LogError("glass pass render");
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
          //  if (dest == RenderTargetHandle.CameraTarget)
          //  {
                cmd.ReleaseTemporaryRT(m_temporaryColorTexture.id);
                cmd.ReleaseTemporaryRT(blurredID.id);
                cmd.ReleaseTemporaryRT(blurredID2.id);
          //  }
        }
    }





    public override void Create()
    {
        m_ScriptablePass = new GlassBlurRenderPass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderEvent;


    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

      //m_ScriptablePass.Setup(src, this.dest);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}