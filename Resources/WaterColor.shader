//
// CinemaPaint
//
// MIT License
// Copyright (c) 2021 Tasuku TAKAHASHI
//
Shader "Hidden/CinemaPaint/PostProcess/WaterColor"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float2 wobblingUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };


			float4 ColorMod(float4 c, float d) {
				return c - (c - c * c) * (d - 1);
			}
        ENDHLSL
        
        Pass
        {
            Name "Wobbling"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment


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
                
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 wobblingColor = LOAD_TEXTURE2D(_WobblingTexture, input.wobblingUV * _ScreenSize.xy);
                float _WobbPower = 0.005;
                float2 wobbling = (wobblingColor.wy * 2 - 1) * _WobbPower;
                
                //return wobblingColor;
                
                return LOAD_TEXTURE2D_X(_SourceTexture, (input.uv + wobbling) * _ScreenSize.xy);
                
                //return 1;
            }
            ENDHLSL
        }
        
        
        Pass
        {
            Name "EdgeDarkning"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment


            float _EdgePower;
            float _EdgeSize;

            float4  _InputTexture_TexelSize;
            TEXTURE2D(_InputTexture);
            
            //TEXTURE2D(_PaperTexture1);
            //TEXTURE2D(_PaperTexture2);
            //TEXTURE2D(_PaperTexture3);
            //SAMPLER(sampler_InputTexture);
            
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.wobblingUV = float2(0,0);
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
        
    }
    Fallback Off
}
