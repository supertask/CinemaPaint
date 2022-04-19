//
// CinemaPaint
//
// MIT License
// Copyright (c) 2022 Tasuku TAKAHASHI
//
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;

using GraphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat;
using SerializableAttribute = System.SerializableAttribute;


namespace CinemaPaint.PostProcessing
{
    [System.Serializable, VolumeComponentMenu("Post-processing/CinemaPaint/WaterColor")]
    public sealed class WaterColor : CustomPostProcessVolumeComponent, IPostProcessComponent
    {        
        public Bool​Parameter isEnabled = new Bool​Parameter(false);

        public Vector2Parameter wobblingTiling = new Vector2Parameter(new Vector2(1,1));
        public Vector2Parameter paperTiling1 = new Vector2Parameter(new Vector2(1,1));
        public ClampedFloatParameter wobblingPower = new ClampedFloatParameter(0.005f, 0, 0.01f);
        public ClampedFloatParameter edgeDarkningPower = new ClampedFloatParameter(1.0f, 0, 1.0f);
        public ClampedFloatParameter paperPower = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);


        //public Bool​Parameter isCircle = new Bool​Parameter(false);
        ///public Vector3Parameter position = new Vector3Parameter(new Vector3(0,0,1));
        //public ClampedFloatParameter power = new ClampedFloatParameter(0, 0, 1.0f);

		public const int PASS_WOBB = 0;
		public const int PASS_EDGE = 1;
		public const int PASS_PAPER = 2;

        Material _material;
        Texture2D _wobblingTexture;
        Texture2D _paperTexture;

        RTHandle wobblingRT;
        RTHandle edgeDarkningRT;

        MaterialPropertyBlock _prop;
        
        private int _baseWidth, _baseHeight;
        int wobblingPass;
        int edgeDarkningPass;

        static class ShaderIDs
        {
            internal static int TempTexture1 = Shader.PropertyToID("_TempTexture1");

            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");

            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");

            internal static readonly int WobblingPower = Shader.PropertyToID("_WobblingPower");
            internal static readonly int WobblingTiling = Shader.PropertyToID("_WobblingTiling");
            internal static readonly int WobblingTexture = Shader.PropertyToID("_WobblingTexture");
            internal static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
            internal static readonly int EdgeSize = Shader.PropertyToID("_EdgeSize");
            
            internal static readonly int PaperTiling = Shader.PropertyToID("_PaperTiling");
            internal static readonly int PaperTexture = Shader.PropertyToID("_PaperTexture");
        }

        public bool IsActive() => _material != null && isEnabled.value; //TODO

        public override CustomPostProcessInjectionPoint injectionPoint =>
        //    CustomPostProcessInjectionPoint.BeforePostProcess;
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            _material = CoreUtils.CreateEngineMaterial("Hidden/CinemaPaint/PostProcess/WaterColor");
            _wobblingTexture =  Resources.Load<Texture2D>("Texture/WaterColorFilter/Wobbling_Seamless");
            _paperTexture =  Resources.Load<Texture2D>("Texture/WaterColorFilter/Shiroishi_washi_letter_paper_Seamless");
            _prop = new MaterialPropertyBlock();
            
            //const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;
            //this.wobbling = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
            wobblingPass = _material.FindPass("Wobbling");
            edgeDarkningPass = _material.FindPass("EdgeDarkning");

        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle srcRT, RTHandle destRT)
        {
            if (_material == null) return;

            //Ref. https://github.com/keijiro/Kino/blob/master/Packages/jp.keijiro.kino.post-processing/Runtime/Streak.cs
            //Ref. https://github.com/alelievr/HDRP-Custom-Passes/blob/master/Assets/CustomPasses/Blur/SlightBlur.cs

            const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;
            
            if (wobblingRT == null) {
                this.wobblingRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
            }
            if (edgeDarkningRT == null) {
                this.edgeDarkningRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
            }

            // Blit Wobbling
            _material.SetFloat(ShaderIDs.WobblingPower, wobblingPower.value);
            _material.SetVector(ShaderIDs.WobblingTiling, wobblingTiling.value);
            _material.SetTexture(ShaderIDs.WobblingTexture, _wobblingTexture);
            _material.SetTexture(ShaderIDs.SourceTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, _material, wobblingRT, _prop, PASS_WOBB);
            //CoreUtils.SetRenderTarget(cmd, wobbling, ClearFlag.Color);
            //CoreUtils.DrawFullScreen(cmd, _material, shaderPassId: wobblingPass, properties: null);

            //if (wobblingRT != null) RTHandles.Release(this.wobblingRT);
            //if (edgeDarkningRT != null) RTHandles.Release(this.edgeDarkningRT);

            // Blit Edge Darkning
            _prop.SetTexture(ShaderIDs.InputTexture, wobblingRT); //ここでのinput textureはshader側でTEXTURE2D_X()で指定しないと読み込まれない
            _material.SetFloat(ShaderIDs.EdgeSize, 1.0f);
            _material.SetFloat(ShaderIDs.EdgePower, 3.0f);
            //_material.SetTexture(ShaderIDs.InputTexture, wobblingRT);
            //_material.SetFloat(ShaderIDs.EdgeSize, 1.0f);
            //_material.SetFloat(ShaderIDs.EdgePower, 3.0f);
            HDUtils.DrawFullScreen(cmd, _material, destRT, _prop, PASS_EDGE);
            
            /*
            _material.SetTexture(ShaderIDs.InputTexture, edgeDarkningRT);
            _material.SetTexture(ShaderIDs.PaperTexture, _paperTexture);
            _material.SetVector(ShaderIDs.PaperTiling, paperTiling1.value);
            
            HDUtils.DrawFullScreen(cmd, _material, destRT, null, PASS_PAPER);  
            */
        }
        
        /*
        protected override void Execute(CustomPassContext ctx)
        {
            if (_material == null) return;
            ctx.propertyBlock.SetVector(ShaderIDs.WobblingTiling, wobblingTiling.value);
            ctx.propertyBlock.SetTexture(ShaderIDs.WobblingTexture, _wobblingTexture);
            ctx.propertyBlock.SetTexture(ShaderIDs.InputTexture, srcRT);
            CoreUtils.SetRenderTarget(ctx.cmd, wobbling, ClearFlag.Color);
            //HDUtils.DrawFullScreen(ctx.cmd, _material, wobbling, null, PASS_WOBB);
            CoreUtils.DrawFullScreen(ctx.cmd, _material, shaderPassId: wobblingPass, properties: ctx.propertyBlock);

            ctx.propertyBlock.SetTexture(ShaderIDs.InputTexture, wobbling);
            //HDUtils.DrawFullScreen(cmd, _material, destRT, _prop, PASS_EDGE);
            CoreUtils.DrawFullScreen(ctx.cmd, _material, shaderPassId: edgeDarkningPass, properties: ctx.propertyBlock);
        }
        */
        
        bool IsSameSize(HDCamera camera) {
            return _baseWidth == camera.actualWidth && _baseHeight == camera.actualHeight;
        }

		void Swap(ref RTHandle src, ref RTHandle dst) {
			var tmp = src;
			src = dst;
			dst = tmp;
		}

        public override void Cleanup()
        {
            CoreUtils.Destroy(_material);
            if (wobblingRT != null) RTHandles.Release(this.wobblingRT);
            if (edgeDarkningRT != null) RTHandles.Release(this.edgeDarkningRT);

        }
    }
}
