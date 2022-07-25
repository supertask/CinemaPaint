//
// Cinema Paint: Artistic Water Color
//
// MIT License
// Copyright (c) 2022 Tasuku TAKAHASHI
// Copyright (c) 2019 t-takasaka, https://github.com/t-takasaka/UnityPostEffectsLibrary/blob/master/LICENSE
//
// Paper: https://yunfei.work/watercolor/watercolor_pp.pdf
//
Shader "Hidden/CinemaPaint/PostProcess/ArtisticWaterColor"
{
	
	Properties  
    {
        //_MainTex("Texture", 2D) = "white"{}  
    }


    SubShader
    {
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #include "Common.hlsl"
            #include "PaintNoise.hlsl"
            #include "Color.hlsl"
            #include "Canvas.hlsl"
            #include "Edge.hlsl"
            #include "Smooth.hlsl"
            #include "BF.hlsl"
            #include "ArtisticWaterColor.hlsl"
            //#include "StrokeBasedOilPaint.hlsl"

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                
				//float aspect = _ScreenSize.x / _ScreenSize.y;
                //output.wobblingUV = output.uv * float2(aspect, 1) * _WobblingTiling.xy;
                //output.wobblingUV = repeatTexture2D(output.wobblingUV);
                return output;
            }

        ENDHLSL
        
        
		Pass
		{
			Name "Entry"

            HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragEntry
			//#pragma fragment fragMask
			//float4 fragMask(Varyings i) : SV_Target{ return 1.0; }

			ENDHLSL
		}

//		Pass
//		{
//			Stencil
//			{
//				Ref 3
//				Comp Equal
//			}
//
//			Name "MaskFace"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragMask
//			float4 fragMask(Varyings i) : SV_Target{ return 1.0; }
//			ENDHLSL
//		}
//		Pass
//		{
//			Stencil
//			{
//				Ref 1
//				Comp Equal
//			}
//
//			Name "MaskBody"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragMask
//			float4 fragMask(Varyings i) : SV_Target{
//				return 0.5;
//			}
//			ENDHLSL
//		}


//		Pass
//		{
//			Name "SBR"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment FragmentStrokeBasedOilPaint
//			ENDHLSL
//		}
		Pass
		{
			Name "WCR"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment FragmenArtistictWaterColor
			ENDHLSL
		}
		Pass
		{
			Name "HandTremor"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment FragmentHandTremor
			ENDHLSL
		}
		Pass
		{
			Name "BF"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragBF
			ENDHLSL
		}


//		Pass
//		{
//			Name "Posterize"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragPosterize
//			ENDHLSL
//		}
		Pass
		{
			Name "Outline"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragOutline
			ENDHLSL
		}

        
		Pass
		{
			Name "TangentFlowMap"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragTFM
			ENDHLSL
		}

		Pass
		{
			Name "SobelFilter"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragSobel3
			ENDHLSL
		}
//		Pass
//		{
//			Name "GBlur"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragGBlur
//			ENDHLSL
//		}
//		Pass
//		{
//			Name "GBlur2"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragGBlur2
//			ENDHLSL
//		}
        
        
		Pass
		{
			Name "RGB2LAB"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragRGB2LAB
			ENDHLSL
		}
		Pass
		{
			Name "LAB2RGB"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragLAB2RGB
			ENDHLSL
		}
		

//		Pass
//		{
//			Name "GNoise"
//			HLSLPROGRAM
//			#pragma vertex Vertex
//			#pragma fragment fragGNoise
//			ENDHLSL
//		}
		Pass
		{
			Name "SNoise"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragSNoise
			ENDHLSL
		}
		
		Pass
		{
			Name "MaskCameraDepthTexture"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragDepthTexture
			float4 fragDepthTexture(Varyings i) : SV_Target{
				return maskDepth(i.uv);
			}
			ENDHLSL
		}
		
		Pass
		{
			Name "BlitTextureX"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragmentCopyTexture
			float4 fragmentCopyTexture(Varyings i) : SV_Target{ return smplX(i.uv); }
			ENDHLSL
		}
		
		Pass
		{
			Name "Debug"
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment fragDebug
			//float4 fragDebug(Varyings i) : SV_Target{ return smplX(i.uv); }
			float4 fragDebug(Varyings i) : SV_Target{ return smpl(i.uv); }
			//float4 fragDebug(Varyings i) : SV_Target{ return smpl(_RT_MASK, i.uv); }
			//float4 fragDebug(Varyings i) : SV_Target{ return 1; }
			ENDHLSL
		}
    }
    Fallback Off
}
