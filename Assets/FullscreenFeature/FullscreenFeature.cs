using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

public class FullscreenFeature : ScriptableRendererFeature
{
  //  public Settings settings = new Settings(); // ����
    FullscreenPass blitPass; // �����Pass
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    public Material blitMaterial = null;
    public float Saturation;
    public float Contrast;



    public class FullscreenPass : ScriptableRenderPass
    {
        // public FullscreenFeature settings; // ������

        private Material bMaterial ;
        private string profilerTag; // ��������ǩ, ��Frame Debugger�п��Կ����ñ�ǩ
        private RenderTargetIdentifier source; // Դ�����ʶ
        private RenderTargetIdentifier destination; // Ŀ�껺���ʶ
        private int destinationId; // Ŀ�껺��id
        private FilterMode filterMode; // ��������˲�ģʽ, ȡֵ��: Point��Bilinear��Trilinear
        public float _Contrast;
        public float _Saturation;

        public FullscreenPass(string tag , Material material,float Contrast ,float Saturation)
        {
            profilerTag = tag;
            bMaterial = material; // ������Ĳ��ʸ�ֵ��bMaterial
            _Contrast = Contrast;
            _Saturation = Saturation;
            destinationId = Shader.PropertyToID("_TempRT");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { // ��Ⱦǰ�ص�
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
            cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
            destination = new RenderTargetIdentifier(destinationId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        { // ִ����Ⱦ
            bMaterial.SetFloat("_Saturation", _Saturation);
            bMaterial.SetFloat("_Contrast", _Contrast);
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            Blit(cmd, source, destination, bMaterial);
            Blit(cmd, destination, source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        { // ��Ⱦ��ص�
            if (destinationId != -1)
            {
                cmd.ReleaseTemporaryRT(destinationId);
            }
        }
    }









    public override void Create()
    { // ��������Pass(�Զ��ص�)

        blitPass = new FullscreenPass(name, blitMaterial , Contrast, Saturation);
      //  blitPass._Contrast = Contrast;
      //  blitPass._Saturation = Saturation;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    { // �����ȾPass(�Զ��ص�)
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
    //{ // ������
    //    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    //    public Material blitMaterial = null;
   // }
}