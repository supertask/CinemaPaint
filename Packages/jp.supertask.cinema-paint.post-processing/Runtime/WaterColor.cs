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
        public Vector2Parameter wobblingTiling = new Vector2Parameter(new Vector2(1,1));

        //public Bool​Parameter isCircle = new Bool​Parameter(false);
        ///public Vector3Parameter position = new Vector3Parameter(new Vector3(0,0,1));
        //public ClampedFloatParameter power = new ClampedFloatParameter(0, 0, 1.0f);

		public const int PASS_WOBB = 0;
		public const int PASS_EDGE = 1;
		public const int PASS_PAPER = 2;

        Material _material;
        Texture2D _wobblingTexture;
        RTHandle wobbling;
        MaterialPropertyBlock _prop;
        
        private int _baseWidth, _baseHeight;
        int wobblingPass;
        int edgeDarkningPass;

        static class ShaderIDs
        {
            internal static int TempTexture1 = Shader.PropertyToID("_TempTexture1");

            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");

            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");

            internal static readonly int WobblingTiling = Shader.PropertyToID("_WobblingTiling");
            internal static readonly int WobblingTexture = Shader.PropertyToID("_WobblingTexture");
            internal static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
            internal static readonly int EdgeSize = Shader.PropertyToID("_EdgeSize");
            
            internal static readonly int PaperTexture1 = Shader.PropertyToID("_PaperTexture1");
            internal static readonly int PaperTexture2 = Shader.PropertyToID("_PaperTexture2");
            internal static readonly int PaperTexture3 = Shader.PropertyToID("_PaperTexture3");
        }

        public bool IsActive() => _material != null && true; //TODO

        public override CustomPostProcessInjectionPoint injectionPoint =>
        //    CustomPostProcessInjectionPoint.BeforePostProcess;
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            _material = CoreUtils.CreateEngineMaterial("Hidden/CinemaPaint/PostProcess/WaterColor");
            _wobblingTexture =  Resources.Load<Texture2D>("Texture/WaterColorFilter/Wobbling");
            //_wobblingTexture =  Resources.Load<Texture2D>("Texture/WaterColorFilter/TurbulenceFLowLayer");
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
            
            if (! IsSameSize(camera)) {
                const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;
                this.wobbling = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
            }

            // Blit Wobbling
            _material.SetVector(ShaderIDs.WobblingTiling, wobblingTiling.value);
            _material.SetTexture(ShaderIDs.WobblingTexture, _wobblingTexture);
            _material.SetTexture(ShaderIDs.SourceTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, _material, wobbling, null, PASS_WOBB);
            //CoreUtils.SetRenderTarget(cmd, wobbling, ClearFlag.Color);
            //CoreUtils.DrawFullScreen(cmd, _material, shaderPassId: wobblingPass, properties: null);
            
            // Blit Edge Darkning
            //_prop.SetTexture(ShaderIDs.InputTexture, wobbling); //ここでのinput textureはshader側でTEXTURE2D_X()で指定しないと読み込まれない
            //_prop.SetFloat(ShaderIDs.EdgeSize, 1.0f);
            //_prop.SetFloat(ShaderIDs.EdgePower, 3.0f);

            _material.SetTexture(ShaderIDs.InputTexture, wobbling);
            _material.SetFloat(ShaderIDs.EdgeSize, 1.0f);
            _material.SetFloat(ShaderIDs.EdgePower, 3.0f);
            
            HDUtils.DrawFullScreen(cmd, _material, destRT, null, PASS_EDGE);
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
            if (wobbling != null) RTHandles.Release(this.wobbling);
        }
    }
}
