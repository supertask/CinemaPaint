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
    [System.Serializable, VolumeComponentMenu("Post-processing/CinemaPaint/ArtisticWaterColor")]
    public partial class ArtisticWaterColor : CustomPostProcessVolumeComponent, IPostProcessComponent
    {        
        public Bool​Parameter isEnabled = new Bool​Parameter(false);

        public ClampedFloatParameter bleeding = new ClampedFloatParameter(40.0f,  0.1f, 40.0f);
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f,  0.0f, 1.0f);
        public ClampedFloatParameter handTremorWaveLen1 = new ClampedFloatParameter(5.0f,  0.0f, 100.0f);
        public ClampedFloatParameter handTremorAmplitude1 = new ClampedFloatParameter(20.0f,  0.0f, 100.0f);
        public ClampedFloatParameter handTremorWaveLen2 = new ClampedFloatParameter(0.0f,  0.0f, 100.0f);
        public ClampedFloatParameter handTremorAmplitude2 = new ClampedFloatParameter(0.0f,  0.0f, 100.0f);
        public ClampedFloatParameter handTremorLen = new ClampedFloatParameter(10.0f,  0.0f, 30.0f);
        public ClampedFloatParameter handTremorScale = new ClampedFloatParameter(1.0f,  0.0f, 3.0f);
        public ClampedIntParameter handTremorDrawCount = new ClampedIntParameter(16,  0, 32);
        public ClampedIntParameter handTremorOverlapCount = new ClampedIntParameter(2,  2, 4);
        public ClampedFloatParameter pigmentDispersionScale = new ClampedFloatParameter(1.5f,  0.0f, 4.0f); //1.0がデフォルトだったが 1.5の方がいい気もする. TODO(Tasuku): 後で調整
        public ClampedFloatParameter turbulenceFowWaveLen1 = new ClampedFloatParameter(2.0f,  0.0f, 4.0f);
        public ClampedFloatParameter turbulenceFowAmplitude1 = new ClampedFloatParameter(120.0f,  0.0f, 300.0f);
        public ClampedFloatParameter turbulenceFowScale1 = new ClampedFloatParameter(1.5f,  0.0f, 4.0f);

        // Hide later
        public ClampedFloatParameter turbulenceFowWaveLen2 = new ClampedFloatParameter(0.0f,  0.0f, 50.0f);
        public ClampedFloatParameter turbulenceFowAmplitude2 = new ClampedFloatParameter(0.0f,  0.0f, 300.0f);
        public ClampedFloatParameter turbulenceFowScale2 = new ClampedFloatParameter(0.0f,  0.0f, 40.0f);
        // =================

        public ClampedFloatParameter edgeDarkingSize = new ClampedFloatParameter(0.1f,  0.0f, 1.0f);
        
        // Hide later
        public ClampedFloatParameter edgeDarkingScale = new ClampedFloatParameter(0.5f,  0.1f, 1.0f);
        public ClampedFloatParameter edgeDarkingLenRatio = new ClampedFloatParameter(1.0f,  0.001f, 1.0f);
        public Bool​Parameter wetInWetDarkToLight = new Bool​Parameter(true);
        // ===============

        public ClampedFloatParameter wetInWetHueSimilarity = new ClampedFloatParameter(10.0f,  0.0f, 180.0f);
        public ClampedFloatParameter wdgeDarkingLenRatio = new ClampedFloatParameter(10.0f,  0.0f, 180.0f);
        public ClampedFloatParameter wetInWetLow = new ClampedFloatParameter(0.0f,  0.0f, 1.0f);
        public ClampedFloatParameter wetInWetHigh = new ClampedFloatParameter(0.65f,  0.0f, 1.0f);
        public ClampedFloatParameter wetInWetWaveLen = new ClampedFloatParameter(300.0f,  0.0f, 300.0f);
        public ClampedFloatParameter wetInWetAmplitude = new ClampedFloatParameter(20.0f,  0.0f, 40.0f);
        public ClampedFloatParameter wetInWetLenRatio = new ClampedFloatParameter(0.5f,  0.001f, 1.0f);
        public ClampedFloatParameter noiseUpdateTime = new ClampedFloatParameter(0.0333f,  0.0f, 10.0f);

        public ClampedFloatParameter wrinkleWaveLen = new ClampedFloatParameter(20.0f,  0.0f, 40.0f);
        public ClampedFloatParameter wrinkleAmplitude = new ClampedFloatParameter(5.0f,  0.0f, 10.0f);
        
        public RenderTextureParameter tex1 = new RenderTextureParameter(null);


        //public Bool​Parameter isCircle = new Bool​Parameter(false);
        ///public Vector3Parameter position = new Vector3Parameter(new Vector3(0,0,1));
        //public ClampedFloatParameter power = new ClampedFloatParameter(0, 0, 1.0f);

		public const int PASS_WOBB = 0;
		public const int PASS_EDGE = 1;
		public const int PASS_PAPER = 2;
        private readonly float CARRY_DIGIT = 10000.0f;

        Material _material;
        Texture2D _wobblingTexture;
        
        Texture2D _washiPaperTexture;
        Texture2D _turbulenceFlowTexture;


        RTHandle snoiseRT1;
        RTHandle snoiseRT2;
        RTHandle[] workRTs;
        RTHandle originRT, mainWorkRT, maskRT, sobelRT,  tangentFlowMapRT, bilateralFilterRT;

        MaterialPropertyBlock _prop;
        
        private int _baseWidth, _baseHeight;
        int blitTexturePass, blitTextureXPass, entryPass, maskCameraDepthTexturePass, maskBodyPass, maskFace, sobelPass, snoisePass, tangentFlowMapPass, bilateralFilterPass,
            RGB2LABPass, LAB2RGBPass, handTremorPass, waterColorPass, debugPass;
        

        public bool IsActive() => _material != null && (
            isEnabled.value
            //wobblingPower.value > 0 || edgeDarkningPower.value > 0 ||
            //washiPaperPower.value > 0 || turbulenceFlowPower.value > 0
        );

        public override CustomPostProcessInjectionPoint injectionPoint =>
        //    CustomPostProcessInjectionPoint.BeforePostProcess;
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            _material = CoreUtils.CreateEngineMaterial("Hidden/CinemaPaint/PostProcess/ArtisticWaterColor");

            _prop = new MaterialPropertyBlock();
            workRTs = new RTHandle[7];
            
            //const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;
            //this.wobbling = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
            blitTexturePass = _material.FindPass("BlitTexture");
            blitTextureXPass = _material.FindPass("BlitTextureX");
            entryPass = _material.FindPass("Entry");
            maskCameraDepthTexturePass = _material.FindPass("MaskCameraDepthTexture");
            maskBodyPass = _material.FindPass("MaskBody");
            sobelPass = _material.FindPass("SobelFilter");
            snoisePass = _material.FindPass("SimplexNoise");
            bilateralFilterPass = _material.FindPass("BF");
            tangentFlowMapPass = _material.FindPass("TangentFlowMap");
            RGB2LABPass = _material.FindPass("RGB2LAB");
            LAB2RGBPass = _material.FindPass("LAB2RGB");

            handTremorPass = _material.FindPass("HandTremor");
            waterColorPass = _material.FindPass("WCR");
            debugPass = _material.FindPass("Debug");
        }
        


        //srcRTはおそらくtexture2Darray
        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle srcRT, RTHandle destRT)
        {
            if (_material == null) return;
            
            
            this.UpdateParameters();

            //Ref. https://github.com/keijiro/Kino/blob/master/Packages/jp.keijiro.kino.post-processing/Runtime/Streak.cs
            //Ref. https://github.com/alelievr/HDRP-Custom-Passes/blob/master/Assets/CustomPasses/Blur/SlightBlur.cs
            //Ref. https://github.com/Unity-Technologies/FPSSample/blob/master/Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Utility/HDUtils.cs

            const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat; //R32G32B32A32_SFloat
            
            if (!IsSameSize(camera))
            {
                this.ReleaseRT();
                this.snoiseRT1 = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                this.snoiseRT2 = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                
                this.originRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                this.mainWorkRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                for(int i = 0; i < workRTs.Length; i++) {
                    this.workRTs[i] = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                }

                this.maskRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                this.sobelRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                this.bilateralFilterRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);
                this.tangentFlowMapRT = RTHandles.Alloc(camera.actualWidth, camera.actualHeight, colorFormat: RTFormat);

                _baseWidth = camera.actualHeight;
                _baseHeight = camera.actualHeight;
            }

            //Texture2DArray to Texture2D
            this._prop.SetTexture(ShaderIDs.InputTextureX, srcRT);
            HDUtils.DrawFullScreen(cmd, this._material, originRT, this._prop, this.blitTextureXPass);
            

            Entry(cmd, srcRT, mainWorkRT);
            RunSNoise(cmd, waterColor.SNoise1, snoiseRT1);
            RunSNoise(cmd, waterColor.SNoise2, snoiseRT2);
            RunBilateralFilter(cmd, mainWorkRT, workRTs[6]); //shader.GetRT(shader.RT_WORK0)
            Swap(ref mainWorkRT, ref workRTs[6]);

            //RunDebug(cmd, snoiseRT1, destRT);
            //RunDebug(cmd, sobelRT, destRT);
            //RunDebug(cmd, tangentFlowMapRT, destRT);
            //RunDebug(cmd, workRTs[3], destRT);
            //RunDebug(cmd, workRTs[3], destRT);
            //RunDebug(cmd, workRTs[6], destRT);
            //RunDebug(cmd, mainWorkRT, destRT);

            this.RenderWaterColor(cmd, mainWorkRT, destRT);
            //this.RenderHandTremor(workRTs[6], mainWorkRT);
            //Swap(this.tempRT4, tempRT0);
            //RunDebug(cmd, workRTs[4], destRT); //Only thmor
            //RunDebug(cmd, workRTs[2], destRT); //all
            //RunDebug(cmd, workRTs[6], destRT); //input
            //RunDebug(cmd, maskRT, destRT);
            //RunDebug(cmd, workRTs[2], destRT);
            //RunDebug(cmd, sobelRT, destRT);
            //RunDebug(cmd, tangentFlowMapRT, destRT);
        }        

        private void Entry(CommandBuffer cmd, RTHandle srcRT, RTHandle destRT)
        {
            //
            // 色補正
            //
            //this._material.SetTexture(ShaderIDs.InputTexture, srcRT);
            //this._material.SetTexture("_RT_MASK", srcRT);
            //this._material.SetFloat("_CCInBlack", InsCC.InputBlack);
            //this._material.SetFloat("_CCInGamma", InsCC.InputGamma);
            //this._material.SetFloat("_CCInWhite", InsCC.InputWhite);
            //this._material.SetFloat("_CCOutBlack", InsCC.OutputBlack);
            //this._material.SetFloat("_CCOutWhite", InsCC.OutputWhite);
            //this._material.SetFloat("_CCMulLum", InsCC.MulLum);
            //this._material.SetFloat("_CCAddLum", InsCC.AddLum);
            
            for(int i = 0; i < workRTs.Length; i++)
            {
                this._material.SetTexture("_RT_WORK" + i, this.workRTs[i]);
            }
            
            // Entry: Color modification
            this._material.SetTexture(ShaderIDs.InputTextureX, srcRT); //最初はMaterialで, そのあとはpropで
            HDUtils.DrawFullScreen(cmd, this._material, mainWorkRT, null, this.entryPass);

            // Generate Mask
            HDUtils.DrawFullScreen(cmd, this._material, maskRT, this._prop, this.maskCameraDepthTexturePass);
            //HDUtils.DrawFullScreen(cmd, this._material, maskRT, this._prop, this.maskBodyPass); // for stencil buffer
        }
        
        private void RunSNoise(CommandBuffer cmd, SNoise snoise, RTHandle destRT)
        {
            // ノイズ生成の負荷が大きいので毎フレーム呼ばないようにする
            //timeElapsedWCR += Time.deltaTime;
            //if(timeElapsedWCR >= waterColor.SNoiseUpdateTime)
            //{ 
            //    timeElapsedWCR = 0.0f;
            //}
            
            this._prop.SetVector("_SNOIZE_SIZE",  snoise.Size);
            this._prop.SetVector("_SNOIZE_SCALE", snoise.Scale);
            this._prop.SetVector("_SNOIZE_SPEED", snoise.Speed);
            HDUtils.DrawFullScreen(cmd, this._material, destRT, this._prop, this.snoisePass);
        }
        
        private void SST(CommandBuffer cmd, RTHandle srcRT)
        {
            //
            // Sobel. 良好(2022/07/19)
            //
            this._prop.SetFloat("_SobelCarryDigit", CARRY_DIGIT); // 桁上げして精度を高める
            this._prop.SetTexture(ShaderIDs.InputTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, this._material, sobelRT, this._prop, this.sobelPass);

            this._prop.SetTexture(ShaderIDs.SobelTexture, sobelRT); // 後段のためにRTを登録しておく
            this._prop.SetFloat("_SobelInvCarryDigit", 1.0f / CARRY_DIGIT); // 後段のために桁下げを登録しておく

            //
            // GBlur
            //
            //TODO(Tasuku): 時間あればやる．もしかしたら必要かも！！！！！！！！！！！！！！
            //shader.UpdateGBlur(gblur); 
            //shader.RenderGBlur(shader.RT_SOBEL, shader.RT_WORK0, gblur); 

            //
            // Tangent Flow Map 良好(2022/07/19)
            //
            this._prop.SetTexture(ShaderIDs.InputTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, this._material, tangentFlowMapRT, this._prop, this.tangentFlowMapPass);
            this._prop.SetTexture("_RT_TFM", tangentFlowMapRT); // 後段のためにRTを登録しておく
            
        }


        private void RunBilateralFilter(CommandBuffer cmd, RTHandle srcRT, RTHandle destRT)
        {
            SST(cmd, srcRT);
            
            // RGB to LAB
            this._prop.SetTexture(ShaderIDs.InputTexture, originRT);
            HDUtils.DrawFullScreen(cmd, this._material, mainWorkRT, this._prop, this.RGB2LABPass);
            
            //
            // Bilateral Filter
            //
            this._prop.SetFloat("_BFSampleLen", bilateralFilter.SampleLen);
            this._prop.SetFloat("_BFDomainVariance", bilateralFilter.DomainVariance);
            this._prop.SetFloat("_BFDomainBias", bilateralFilter.DomainBias);
            this._prop.SetFloat("_BFRangeVariance", bilateralFilter.RangeVariance);
            this._prop.SetFloat("_BFRangeBias", bilateralFilter.RangeBias);
            this._prop.SetFloat("_BFRangeThreshold", bilateralFilter.RangeThreshold);
            this._prop.SetFloat("_BFStepDirScale", bilateralFilter.StepDirScale);
            this._prop.SetFloat("_BFStepLenScale", bilateralFilter.StepLenScale);
            
            /*
            Debug.LogFormat("SampleLen = {0}, DomainVariance = {1}, DomainBias = {2}, RangeVariance = {3}, RangeBias = {4}, RangeThreshold = {5}, StepDirScale = {6}, StepLenScale = {7}",
                bilateralFilter.SampleLen, bilateralFilter.DomainVariance, bilateralFilter.DomainBias,
                bilateralFilter.RangeVariance, bilateralFilter.RangeBias, bilateralFilter.RangeThreshold,
                bilateralFilter.StepDirScale, bilateralFilter.StepLenScale);
            */

            //for(int i = 0; i < 256; i++)
            //{
            //    Debug.LogFormat("RangeWeight[{0}] = {1}", i, bilateralFilter.RangeWeight[i]);
            //}
            
            this._prop.SetFloatArray("_BFRangeWeight", bilateralFilter.RangeWeight);
            
            for (int i = 0; i < bilateralFilter.BlurCount; i++)
            {
                this._prop.SetFloat("_BFOrthogonalize", 1.0f);
                this._prop.SetTexture(ShaderIDs.InputTexture, mainWorkRT);
                HDUtils.DrawFullScreen(cmd, this._material, workRTs[4], this._prop, this.bilateralFilterPass);
                
                this._prop.SetFloat("_BFOrthogonalize", 0.0f);
                this._prop.SetTexture(ShaderIDs.InputTexture, workRTs[4]);
                HDUtils.DrawFullScreen(cmd, this._material, workRTs[3], this._prop, this.bilateralFilterPass);
                //workRTs[3] is destination

                //Blit
                this._prop.SetTexture(ShaderIDs.InputTexture, workRTs[3]);
                HDUtils.DrawFullScreen(cmd, this._material, mainWorkRT, this._prop, this.blitTexturePass);
            }

            // LAB to RGB
            this._prop.SetTexture(ShaderIDs.InputTexture, workRTs[3]);
            HDUtils.DrawFullScreen(cmd, this._material, destRT, this._prop, this.LAB2RGBPass);
        }
        
        
        public void RunDebug(CommandBuffer cmd, RTHandle srcRT, RTHandle destRT)
        {
            // Debug
            this._prop.SetTexture(ShaderIDs.InputTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, this._material, destRT, this._prop, this.debugPass);
        }

        
        public void RenderWaterColor(CommandBuffer cmd, RTHandle srcRT, RTHandle destRT)
        {
            // Hand Tremor
            this._prop.SetTexture(ShaderIDs.InputTexture, srcRT);
            this._prop.SetTexture(ShaderIDs.SnoiseTexture, snoiseRT1);
            //this._prop.SetTexture(ShaderIDs.MaskTexture, maskRT); //TODO(Tasuku): !!!!マスクがある領域は処理がされない!!!!
            this._prop.SetFloat("_WCRBleeding", waterColor.Bleeding);
            this._prop.SetFloat("_WCROpacity", waterColor.Opacity);
            this._prop.SetFloat("_WCRHandTremorLen", waterColor.HandTremorLen);
            this._prop.SetFloat("_WCRHandTremorScale", waterColor.HandTremorScale);
            this._prop.SetFloat("_WCRHandTremorDrawCount", waterColor.HandTremorDrawCount);
            this._prop.SetFloat("_WCRHandTremorInvDrawCount", waterColor.HandTremorInvDrawCount);
            this._prop.SetFloat("_WCRHandTremorOverlapCount", waterColor.HandTremorOverlapCount);
            this._prop.SetFloat("_WCRPigmentDispersionScale", waterColor.PigmentDispersionScale);
            this._prop.SetFloat("_WCRTurbulenceFowScale1", waterColor.TurbulenceFowScale1);
            this._prop.SetFloat("_WCRTurbulenceFowScale2", waterColor.TurbulenceFowScale2);
            HDUtils.DrawFullScreen(cmd, this._material, workRTs[4], this._prop, this.handTremorPass);
            
            SST(cmd, workRTs[4]);
            //SST(cmd, srcRT);

            //ここでバグってそう
            // Water color
            this._prop.SetTexture(ShaderIDs.InputTexture, workRTs[4]);
            //this._prop.SetTexture(ShaderIDs.InputTexture, srcRT);
            this._prop.SetTexture(ShaderIDs.MaskTexture, maskRT);
            this._prop.SetTexture(ShaderIDs.TangentFlowMapTexture, tangentFlowMapRT); //必須
            this._prop.SetTexture(ShaderIDs.SobelTexture, sobelRT);
            this._prop.SetTexture(ShaderIDs.SnoiseTexture, snoiseRT2);
            this._prop.SetFloat("_WetInWetLenRatio", waterColor.WetInWetLenRatio);
            this._prop.SetFloat("_WetInWetInvLenRatio", waterColor.WetInWetInvLenRatio);
            this._prop.SetFloat("_WetInWetLow", waterColor.WetInWetLow);
            this._prop.SetFloat("_WetInWetHigh", waterColor.WetInWetHigh);
            this._prop.SetFloat("_WetInWetDarkToLight", waterColor.WetInWetDarkToLight);
            this._prop.SetFloat("_WetInWetHueSimilarity", waterColor.WetInWetHueSimilarity);
            this._prop.SetFloat("_EdgeDarkingLenRatio", waterColor.EdgeDarkingLenRatio);
            this._prop.SetFloat("_EdgeDarkingInvLenRatio", waterColor.EdgeDarkingInvLenRatio);
            this._prop.SetFloat("_EdgeDarkingSize", waterColor.EdgeDarkingSize);
            this._prop.SetFloat("_EdgeDarkingScale", waterColor.EdgeDarkingScale);

            HDUtils.DrawFullScreen(cmd, this._material, destRT, this._prop, this.waterColorPass);
        }        

        
        private void RenderNoises()
        {
            if (Time.frameCount % (60 * 3) == 0)
            {
                //this._material.SetVector("_SNOIZE_SIZE", );

            }

        }



        
        bool IsSameSize(HDCamera camera) {
            return _baseWidth == camera.actualWidth && _baseHeight == camera.actualHeight;
        }

		void Swap(ref RTHandle srcRT, ref RTHandle destRT) {
			var tmp = srcRT;
			srcRT = destRT;
			destRT = tmp;
		}

        public override void Cleanup()
        {
            CoreUtils.Destroy(_material);
            this.ReleaseRT();
        }

        public void ReleaseRT()
        {
            if (snoiseRT1 != null) RTHandles.Release(this.snoiseRT1);
            if (snoiseRT2 != null) RTHandles.Release(this.snoiseRT2);
            for(int i = 0; i < workRTs.Length; i++) {
                if (workRTs[i] != null) RTHandles.Release(this.workRTs[i]);
            }
            if (maskRT != null) RTHandles.Release(this.maskRT);            
            
            if (originRT != null) RTHandles.Release(this.originRT);            
            if (mainWorkRT != null) RTHandles.Release(this.mainWorkRT);            
            if (sobelRT != null) RTHandles.Release(this.sobelRT);            
            if (bilateralFilterRT != null) RTHandles.Release(this.bilateralFilterRT);            
            if (tangentFlowMapRT != null) RTHandles.Release(this.tangentFlowMapRT);            
        }
    }
}
