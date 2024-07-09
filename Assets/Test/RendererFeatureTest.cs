using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RendererFeatureTest : ScriptableRendererFeature
{
    public Material passMaterial;
    public RenderPassEvent renderPassEvent;
    private bool requirerColor;
    private bool isBeforeTransparents;
    public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.Color;
    private MyRenderPass mypass;
    class MyRenderPass : ScriptableRenderPass
    {
        private Material passMaterial;
        private int passIndex = 0;
        private bool requiresColor;
        private bool isBeforeTransparents;
        private PassData passData;
        private RTHandle colorRT;
        private static readonly int _BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");

        public void Setup(Material mat , int index ,bool requiresColor , bool isBeforeTransparents,in RenderingData renderingData)
        {

            passMaterial = mat;
            passIndex = index;
            this.requiresColor = requiresColor;
            this.isBeforeTransparents = this.isBeforeTransparents;
            RenderTextureDescriptor colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            colorCopyDescriptor.depthBufferBits = (int)DepthBits.None;
            RenderingUtils.ReAllocateIfNeeded(ref colorRT, colorCopyDescriptor, name: "MyScreenPassColor");
            passData = new PassData();

        }

        public void Dispose()
        {
            colorRT?.Release();
        }
        // 他的方法在执行渲染通道之前被调用 
        // 它可用于配置呈现目标及其清除状态。 还可以创建临时渲染目标纹理。 
        // 当此渲染通道为空时，将渲染到活动摄像机渲染目标。 
        // 永远不要调用CommandBuffer.SetRenderTarget。 而是调用<c>ConfigureTarget</c>和<c>ConfigureClear</c>。 
        // 渲染管道将确保目标设置和清除以高性能的方式进行。 
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // 在这里，您可以实现呈现逻辑。
        // 使用<c>ScriptableRenderContext</c>发出绘图命令或执行命令缓冲区 
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // 您不必调用ScriptableRenderContext。 提交时，渲染管道将在管道中的特定点调用它。 
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            passData._material = passMaterial;
            passData._PassIndex = passIndex;
            passData._requiresColor = requiresColor;
            passData._isBeforeTransparents = isBeforeTransparents;
            passData._colorRT = colorRT;
            ExecutePass(passData,ref renderingData,ref context);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        private static void ExecutePass(PassData passData,ref RenderingData renderingData,ref ScriptableRenderContext context)
        {
            Material passMaterial = passData._material;
            int passIndex = passData._PassIndex;
            bool requiesColor = passData._requiresColor;
            bool isBeforeTransparents = passData._isBeforeTransparents;
            RTHandle colorRT = passData._colorRT;
            if (passMaterial == null)
            {
                return;
            }

            if (renderingData.cameraData.isPreviewCamera)
            {
                return;
                
            }

            CommandBuffer cmd = CommandBufferPool.Get("MyColorBuffer");
            CameraData cameraData = renderingData.cameraData;
            RTHandle sourceRT = null;
            if (requiesColor)
            {
                if (!isBeforeTransparents)
                {
                   // sourceRT = cameraData.renderer.GetCameraColorBackBuffer(cmd);
                   sourceRT = cameraData.renderer.cameraColorTargetHandle;
                }
                
                CoreUtils.SetRenderTarget(cmd,cameraData.renderer.cameraColorTargetHandle);
                CoreUtils.DrawFullScreen(cmd,passMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
            }



        }
        
        
        
        
    }

    

    /// <inheritdoc/>
    public override void Create()
    {
        mypass = new MyRenderPass();

        // Configures where the render pass should be injected.
        mypass.renderPassEvent = renderPassEvent;
        ScriptableRenderPassInput modifiedRequirements = requirements;
        isBeforeTransparents = mypass.renderPassEvent <= RenderPassEvent.BeforeRenderingTransparents;
        if (requirerColor&& !isBeforeTransparents)
        {
            modifiedRequirements ^= ScriptableRenderPassInput.Color;
        }
        mypass.ConfigureInput(modifiedRequirements);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (passMaterial == null)
        {
            Debug.LogError("没有材质");
            return;
            
        }
        mypass.Setup(passMaterial,0,requirerColor,isBeforeTransparents,renderingData);
        renderer.EnqueuePass(mypass);
    }
}

internal class PassData
{

    public Material _material;
    public int _PassIndex;
    public bool _requiresColor;
    public bool _isBeforeTransparents;
    public RTHandle _colorRT;


}




