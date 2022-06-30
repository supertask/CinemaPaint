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
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #include "ArtisticWaterColor.cginc"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			float4 ColorMod(float4 c, float d) {
				return c - (c - c * c) * (d - 1);
			}
            
            //https://graphtoy.com/?f1(x,t)=abs(mod(x,1.0))&v1=true&f2(x,t)=&v2=false&f3(x,t)=&v3=false&f4(x,t)=&v4=false&f5(x,t)=&v5=false&f6(x,t)=&v6=false&grid=1&coords=10.971303370786517,0.033808988764044756,14.520000000000003
            float repeatTexture1D(float x) {
                return abs(fmod(x,1.0));
            }

            float2 repeatTexture2D(float2 uv) {
                return float2(repeatTexture1D(uv.x), repeatTexture1D(uv.y));
            }
        ENDHLSL
        
        Pass
        {
            Name "Wobbling"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float2 wobblingUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float _WobblingPower;
            float2 _WobblingTiling;
            TEXTURE2D(_WobblingTexture);

            TEXTURE2D_X(_SourceTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                
				float aspect = _ScreenSize.x / _ScreenSize.y;
                output.wobblingUV = output.uv * float2(aspect, 1) * _WobblingTiling.xy;
                //output.wobblingUV = repeatTexture2D(output.wobblingUV);
                
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float4 wobblingColor = SAMPLE_TEXTURE2D(_WobblingTexture, s_linear_repeat_sampler, input.wobblingUV);
                float2 wobbling = (wobblingColor.wy * 2 - 1) * _WobblingPower;                                
                return LOAD_TEXTURE2D_X(_SourceTexture, (input.uv + wobbling) * _ScreenSize.xy);
            }
            ENDHLSL
        }
        
        
        Pass
        {
            Name "EdgeDarkning"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _EdgePower;
            float _EdgeSize;

            float4  _InputTexture_TexelSize;
            TEXTURE2D(_InputTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv_offset = _InputTexture_TexelSize.xy * _EdgeSize;
				float4 src_l = LOAD_TEXTURE2D(_InputTexture, (input.uv + float2(-uv_offset.x, 0)) * _ScreenSize.xy);
				float4 src_r = LOAD_TEXTURE2D(_InputTexture, (input.uv + float2(+uv_offset.x, 0)) * _ScreenSize.xy);
				float4 src_b = LOAD_TEXTURE2D(_InputTexture, (input.uv + float2(           0, -uv_offset.y)) * _ScreenSize.xy );
				float4 src_t = LOAD_TEXTURE2D(_InputTexture, (input.uv + float2(           0, +uv_offset.y)) * _ScreenSize.xy );
				float4 src = LOAD_TEXTURE2D(_InputTexture, input.uv * _ScreenSize.xy );
                
				float4 grad = abs(src_r - src_l) + abs(src_b - src_t);
				float intensity = saturate(0.333 * (grad.x + grad.y + grad.z));
                
				float d = _EdgePower * intensity + 1;
				return ColorMod(src, d);
                
            }
            ENDHLSL
        }
        
        
        Pass
        {
            Name "Paper"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float2 paperUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4  _InputTexture_TexelSize;
            TEXTURE2D(_InputTexture);

			float2 _PaperTiling;
			float _PaperPower;            
            TEXTURE2D(_PaperTexture);
            //SAMPLER(sampler_PaperTexture);

            
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
				float aspect = _ScreenSize.x / _ScreenSize.y;
                output.paperUV = output.uv * float2(aspect, 1) * _PaperTiling.xy;
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                
				float4 srcColor = LOAD_TEXTURE2D(_InputTexture, input.uv * _ScreenSize.xy );
                //return srcColor;
                
                //input.paperUV = repeatTexture2D(input.paperUV);
                //float4 paperColor = LOAD_TEXTURE2D(_PaperTexture, input.paperUV * _ScreenSize.xy);
                float4 paperColor = SAMPLE_TEXTURE2D(_PaperTexture, s_linear_repeat_sampler, input.paperUV);
				float paper = _PaperPower * (paperColor - 0.5) + 1;
                
				return ColorMod(srcColor, paper);
                
            }
            ENDHLSL
        }
        
    }
    Fallback Off
}
