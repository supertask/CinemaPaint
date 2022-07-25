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
        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Color Correction
        //////////////////////////////////////////////////////////////////////////////////////////////////
        public static class InsCC
        {
            [SerializeField, Range(0.0f, 255.0f)] public static float InputBlack = 0.0f;
            [SerializeField, Range(0.0f, 2.0f)] public static float InputGamma = 1.0f;
            [SerializeField, Range(0.0f, 255.0f)] public static float InputWhite = 255.0f;
            [SerializeField, Range(0.0f, 255.0f)] public static float OutputBlack = 0.0f;
            [SerializeField, Range(0.0f, 255.0f)] public static float OutputWhite = 255.0f;
            [SerializeField, Range(0.0f, 2.0f)] public static float MulLum = 1.0f;
            [SerializeField, Range(-1.0f, 1.0f)] public static float AddLum = 0.0f;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Canvas
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Serializable]
        public class InsCanvas
        {
            [SerializeField, Range(0.0f, 40.0f)] internal float WrinkleWaveLen = 20.0f;
            [SerializeField, Range(0.0f, 10.0f)] internal float WrinkleAmplitude = 5.0f;
            [SerializeField, Range(0.0f, 1.0f)] internal float RuledLineDensity = 0.0f;
            [SerializeField, Range(1.0f, 3.0f)] internal float RuledLineSize = 2.0f;
            [SerializeField, Range(0.0f, 90.0f)] internal float RuledLineAngle = 45.0f;
        }
        


        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Bilateral Filter
        //////////////////////////////////////////////////////////////////////////////////////////////////
        public class InsBF
        {
            [SerializeField] internal bool DefaultParameters;
            [HideInInspector][SerializeField] internal bool FlowBased = false;
            internal const int BlurCountMin = 1, BlurCountMax = 20;
            [SerializeField, Range(BlurCountMin, BlurCountMax)] internal int BlurCount = 4;
            [SerializeField, Range(0.1f, 20.0f)] internal float SampleLen = 10.0f;
            [SerializeField, Range(0.1f, 20.0f)] internal float DistanceSigma = 10.0f;
            [SerializeField, Range(0.1f, 2.0f)] internal float DistanceBias = 1.0f;
            [SerializeField, Range(0.1f, 4.0f)] internal float ColorSigma = 2.0f;
            [SerializeField, Range(0.1f, 128.0f)] internal float ColorBias = 64.0f;
            [SerializeField, Range(1.0f, 1024.0f)] internal float ColorThreshold = 4.0f;
            [SerializeField, Range(1.0f, 10.0f)] internal float StepDirScale = 2.0f;
            [SerializeField, Range(1.0f, 4.0f)] internal float StepLenScale = 1.0f;
        }
        

        public class BilateralFilter
        {
            public bool FlowBased;
            public int BlurCount;
            public float SampleLen;
            public float DomainVariance, RangeVariance;
            public float DomainBias, RangeBias;
            public float RangeThreshold;
            public float StepDirScale, StepLenScale;
            public bool UsePreCalc = false;
            public float[] RangeWeight = new float[256];

            public void Set(InsBF bf) 
            { 
                FlowBased = bf.FlowBased;
                BlurCount = bf.BlurCount;
                SampleLen = bf.SampleLen;
                RangeBias = bf.ColorBias;
                RangeThreshold = 1.0f / bf.ColorThreshold;
                DomainBias = bf.DistanceBias;
                // Gσ(x) = exp(−(x^2) / (2 * σ^2)) の (2 * σ^2)
                // 分母として使うので逆数にしておく
                DomainVariance = 1.0f / (bf.DistanceSigma * bf.DistanceSigma * 2.0f);
                RangeVariance = 1.0f / (bf.ColorSigma * bf.ColorSigma * 2.0f);
                StepDirScale = bf.StepDirScale;
                StepLenScale = bf.StepLenScale;

                //if(!UsePreCalc){ return; } //WARNING: これがあると計算されない！！

                for(int i = 0; i < 256; i++)
                {
                    float x = i * RangeBias;
                    RangeWeight[i] = Mathf.Exp(-(x * x) * RangeVariance);
                    //Debug.LogFormat("{0}, {1}", i, RangeWeight[i] );
                }
            }
        }
 
        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Simplex Noise
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Serializable]
        public class InsSNoise
        {
            [SerializeField, Range(1.0f, 256.0f)] internal float Size1 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale1 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed1 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size2 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale2 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed2 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size3 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale3 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed3 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size4 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale4 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed4 = 1.0f;
        }
        public class SNoise
        {
            public Vector4 Size = new Vector4();
            public Vector4 Scale = new Vector4();
            public Vector4 Speed = new Vector4();
            public int RT = 6; //TODO：マジックナンバーやめる
            public void Set(InsSNoise noise)
            {
                Size.Set(noise.Size1, noise.Size2, noise.Size3, noise.Size4);
                Scale.Set(noise.Scale1, noise.Scale2, noise.Scale3, noise.Scale4);
                Speed.Set(noise.Speed1, noise.Speed2, noise.Speed3, noise.Speed4);
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Watercolor Rendering
        //////////////////////////////////////////////////////////////////////////////////////////////////
        public class WaterColorParam
        {
            public float Bleeding, Opacity, HandTremorLen, HandTremorScale;
            public float HandTremorDrawCount, HandTremorInvDrawCount, HandTremorOverlapCount;
            public float PigmentDispersionScale, TurbulenceFowScale1, TurbulenceFowScale2;
            public float WetInWetLenRatio, WetInWetInvLenRatio;
            public float WetInWetLow, WetInWetHigh;
            public float WetInWetDarkToLight, WetInWetHueSimilarity;
            public float EdgeDarkingLenRatio, EdgeDarkingInvLenRatio;
            public float EdgeDarkingEdgeThreshold;
            public float EdgeDarkingSize, EdgeDarkingScale;

            public float SNoiseUpdateTime;
            public SNoise SNoise1 = new SNoise(), SNoise2 = new SNoise();

            public void Set(ArtisticWaterColor awc) 
            {
                Bleeding = awc.bleeding.value;
                Opacity = awc.opacity.value;
                HandTremorLen = awc.handTremorLen.value;
                HandTremorScale = awc.handTremorScale.value;
                HandTremorDrawCount = awc.handTremorDrawCount.value;
                HandTremorInvDrawCount = 1.0f / awc.handTremorDrawCount.value;
                HandTremorOverlapCount = awc.handTremorOverlapCount.value;
                PigmentDispersionScale = awc.pigmentDispersionScale.value; 
                TurbulenceFowScale1 = awc.turbulenceFowScale1.value;
                TurbulenceFowScale2 = awc.turbulenceFowScale2.value;
                WetInWetLenRatio = 1.0f - awc.wetInWetLenRatio.value;
                WetInWetInvLenRatio = 1.0f / awc.wetInWetLenRatio.value;
                WetInWetLow = awc.wetInWetLow.value;
                WetInWetHigh = awc.wetInWetHigh.value;
                WetInWetDarkToLight = awc.wetInWetDarkToLight.value ? 1.0f : 0.0f;
                WetInWetHueSimilarity = awc.wetInWetHueSimilarity.value;
                EdgeDarkingLenRatio = 1.0f - awc.wdgeDarkingLenRatio.value;
                EdgeDarkingInvLenRatio = 1.0f / awc.edgeDarkingLenRatio.value;
                EdgeDarkingSize = awc.edgeDarkingSize.value;
                EdgeDarkingScale = awc.edgeDarkingScale.value;

                /*
                SNoiseUpdateTime = noiseUpdateTime.value;
                SNoise1.Size.Set(handTremorWaveLen1.value, handTremorWaveLen2.value, 
                                    turbulenceFowWaveLen1.value, turbulenceFowWaveLen2.value);
                SNoise1.Scale.Set(handTremorAmplitude1.value, handTremorAmplitude2.value, 
                                    turbulenceFowAmplitude1.value, turbulenceFowAmplitude2.value);
                //SNoise1.Speed.Set(0.1f, 0.1f, 0.1f, 0.1f);
                SNoise1.Speed.Set(0.0f, 0.0f, 0.0f, 0.0f);
                SNoise1.RT = 6;

                SNoise2.Size.Set(wetInWetWaveLen.value, 1.0f, 1.0f, wrinkleWaveLen.value);
                SNoise2.Scale.Set(wetInWetAmplitude.value, 1.0f, 1.0f, wrinkleAmplitude.value);
                //SNoise2.Speed.Set(0.1f, 0.1f, 0.1f, 0.1f);
                SNoise2.Speed.Set(0.0f, 0.0f, 0.0f, 0.0f);
                SNoise2.RT = 7;
                */
            }
        }
   
    

    public partial class ArtisticWaterColor : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        static class ShaderIDs
        {
            internal static int TempTexture1 = Shader.PropertyToID("_TempTexture1");

            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");

            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");
            internal static readonly int InputTextureX = Shader.PropertyToID("_InputTextureX");

            internal static readonly int WobblingPower = Shader.PropertyToID("_WobblingPower");
            internal static readonly int WobblingTiling = Shader.PropertyToID("_WobblingTiling");
            internal static readonly int WobblingTexture = Shader.PropertyToID("_WobblingTexture");
            internal static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
            internal static readonly int EdgeSize = Shader.PropertyToID("_EdgeSize");
            
            internal static readonly int PaperPower = Shader.PropertyToID("_PaperPower");
            internal static readonly int PaperTiling = Shader.PropertyToID("_PaperTiling");
            internal static readonly int PaperTexture = Shader.PropertyToID("_PaperTexture");
        }
        
        

        private InsBF BFParameters = new InsBF();
        private InsSNoise SNoiseParameters = new InsSNoise();
        private BilateralFilter bilateralFilter = new BilateralFilter();
        private WaterColorParam waterColor = new WaterColorParam();
        
        public void UpdateParameters()
        {
            this.bilateralFilter.Set(BFParameters);
            //this.bilateralFilter.Set(SNoiseParameters);
            this.waterColor.Set(this);
        }
    }


}
