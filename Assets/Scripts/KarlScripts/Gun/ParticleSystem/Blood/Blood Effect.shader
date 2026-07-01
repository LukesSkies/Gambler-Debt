Shader "Universal Render Pipeline/Particles/Blood Effect"
{
    Properties
    {
        [Header (Color Controls)]
        [HDR] _BaseColor ("Base Color Mult", Color) = (1,1,1,1)
        _LightStr ("Lighting Strength", float) = 0.85
        _AlphaMin ("Alpha Clip Min", Range (-0.01, 1.01)) = 0.1
        _AlphaSoft ("Alpha Clip Softness", Range (0,1)) = 0.022
        _EdgeDarken ("Edge Darkening", float) = 1.0
        _ProcMask ("Procedural Mask Strength", float) = 1.0

        [Header (Mask Controls)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        _MaskStr ("Mask Strength", float) = 0.7
        _Columns ("Flipbook Columns", Int) = 1
        _Rows ("Flipbook Rows", Int) = 1
        _ChannelMask ("Channel Mask", Vector) = (1,0,0,0)
        [Toggle] _FlipU("Flip U Randomly", float) = 0
        [Toggle] _FlipV("Flip V Randomly", float) = 0

        [Header (Noise Controls)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseAlphaStr ("Noise Strength", float) = 0.8
        _ChannelMask2 ("Channel Mask",Vector) = (1,0,0,0)
        _Randomize ("Randomize Noise", float) = 1.0

        [Header (Specular Reflection)]
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _ReflectionSat ("Reflection Saturation", float) = 0.5
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Normal ("Normal Map", 2D) = "bump" {}
        _FlattenNormal ("Flatten Normal", float) = 1.0
        [Toggle(SPECULAR_REFLECTION_ON)] _UseSpecular("Enable Specular", float) = 1

        [Header (UV Warp)]
        _WarpTex ("Warp Texture", 2D) = "white" {}
        _WarpStr ("Warp Strength", float) = 0.1
        _NoiseColorStr ("Noise Color Strength", float) = 0.5

        [Header (Vertex Physics)]
        _FallOffset ("Gravity Offset", range(-1,0)) = -1.0
        _FallRandomness ("Gravity Randomness", float) = 0.25
    }

    SubShader
    {
        Tags 
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local SPECULAR_REFLECTION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Properties
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _SpecularColor;
                half _LightStr;
                half _AlphaMin;
                half _AlphaSoft;
                half _EdgeDarken;
                half _ProcMask;

                float4 _MainTex_ST;
                half _MaskStr;
                half _Columns;
                half _Rows;
                half4 _ChannelMask;
                half _FlipU;
                half _FlipV;

                float4 _ReflectionTex_ST;
                half _ReflectionSat;

                float4 _NoiseTex_ST;
                half _NoiseAlphaStr;
                half _NoiseColorStr;
                half4 _ChannelMask2;
                half _FlattenNormal;
                half _Randomize;

                float4 _WarpTex_ST;
                half _WarpStr;

                half _FallOffset;
                half _FallRandomness;
            CBUFFER_END

            // Textures
            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_WarpTex);        SAMPLER(sampler_WarpTex);
            #ifdef SPECULAR_REFLECTION_ON
                TEXTURE2D(_ReflectionTex);  SAMPLER(sampler_ReflectionTex);
                TEXTURE2D(_Normal);         SAMPLER(sampler_Normal);
            #endif

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 texcoord0    : TEXCOORD0; // XY = UV, Z = Random, W = Lifetime
                float3 texcoord1    : TEXCOORD1; // X = Pan Offset, Y = UV Warp Strength, Z = Gravity
                float4 color        : COLOR;
                #ifdef SPECULAR_REFLECTION_ON
                    half4 tangentOS : TANGENT;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float4 uv               : TEXCOORD0;
                float4 color            : COLOR;
                float3 customData       : TEXCOORD1; // XY = custom data, Z = stable random
                float3 normalWS         : TEXCOORD2;
                float fogFactor         : TEXCOORD3;
                
                #ifdef SPECULAR_REFLECTION_ON
                    float3 viewDirWS    : TEXCOORD4;
                    float3x3 tangentToWorld : TEXCOORD5;
                #endif
                
                float3 vertLight        : TEXCOORD8;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Calculate gravity fall
                float lifetime = input.texcoord0.w;
                lifetime = lifetime * lifetime + (_FallOffset + ((input.texcoord0.z - 0.5) * _FallRandomness)) * lifetime;
                float3 fallOffset = lifetime * float3(0, input.texcoord1.z, 0);

                // UV flipping
                float2 UVflip = round(frac(float2(input.texcoord0.z * 13, input.texcoord0.z * 8)));
                UVflip = UVflip * 2 - 1;
                UVflip = lerp(1, UVflip, float2(_FlipU, _FlipV));

                // Position with gravity
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz) + fallOffset;
                output.positionCS = TransformWorldToHClip(positionWS);

                // Color
                output.color = input.color;
                output.color.a *= output.color.a;
                output.color.a += _AlphaMin;

                // Normal
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Custom data
                output.customData = float3(input.texcoord1.xy, input.texcoord0.z);

                // UVs
                output.uv.xy = TRANSFORM_TEX(input.texcoord0.xy * UVflip, _MainTex);
                output.uv.zw = output.uv.xy * half2(_Columns, _Rows) + input.texcoord0.z * half2(3, 8) * _Randomize;

                #ifdef SPECULAR_REFLECTION_ON
                    // Tangent space to world space
                    float3 bitangent = cross(input.normalOS, input.tangentOS.xyz) * input.tangentOS.w;
                    float3x3 objectToTangent = float3x3(input.tangentOS.xyz, bitangent, input.normalOS);
                    output.tangentToWorld = mul((float3x3)GetObjectToWorldMatrix(), transpose(objectToTangent));
                    
                    // View direction
                    output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                #endif

                // Vertex lighting (URP style)
                float3 sh = SampleSH(output.normalWS);
                sh = max(sh, 0.15); // Don't go to complete black
                output.vertLight = lerp(1, sh, _LightStr);

                // Fog
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample UV warp
                float4 uvWarp = SAMPLE_TEXTURE2D(_WarpTex, sampler_WarpTex, 
                    input.uv.zw * _WarpTex_ST.xy + _WarpTex_ST.zw * (input.customData.x + 1) + (float2(5, 8) * input.customData.z));
                float2 warp = (uvWarp.xy * 2) - 1;
                warp *= _WarpStr * input.customData.y;

                // Sample mask
                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy * _MainTex_ST.xy + warp);
                mask = saturate(lerp(1, mask, _MaskStr));

                // Edge mask
                half2 tempUV = frac(input.uv.xy * half2(_Columns, _Rows)) - 0.5;
                tempUV *= tempUV * 4;
                half edgeMask = saturate(tempUV.x + tempUV.y);
                edgeMask *= edgeMask;
                edgeMask = 1 - edgeMask;
                edgeMask = lerp(1.0, edgeMask, _ProcMask);

                mask *= edgeMask;
                half4 col = max(0.001, input.color);
                col.a = saturate(dot(mask, _ChannelMask));

                // Sample noise
                half4 noise4 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 
                    input.uv.zw * _NoiseTex_ST.xy + _NoiseTex_ST.zw * input.customData.x + warp);
                half noise = dot(noise4, _ChannelMask2);
                noise = saturate(lerp(1, noise, _NoiseAlphaStr));

                // Alpha clip
                col.a *= noise;
                half preClipAlpha = col.a;
                half clippedAlpha = saturate((preClipAlpha * input.color.a - _AlphaMin) / (_AlphaSoft));
                col.a = clippedAlpha;

                // Base lighting
                float3 baseLighting = input.vertLight;

                #ifdef SPECULAR_REFLECTION_ON
                    // Sample normals
                    half3 normalTex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal,
                        input.uv.zw * _NoiseTex_ST.xy + _NoiseTex_ST.zw * input.customData.x + warp));

                    // Flatten normals near edge
                    normalTex.z = _FlattenNormal * (saturate((preClipAlpha * input.color.a - _AlphaMin) / (_AlphaSoft + 0.2)) - 0.1) * 1.2;
                    normalTex = normalize(normalTex);

                    // Transform to world space
                    normalTex.xyz = mul(input.tangentToWorld, normalTex.xyz);
                    float3 combinedNormals = normalize(input.normalWS + normalTex);

                    // Calculate reflection
                    float3 reflectionVector = reflect(-normalize(input.viewDirWS), combinedNormals);
                    reflectionVector.x = atan2(reflectionVector.x, reflectionVector.z) * 0.31831;
                    reflectionVector = reflectionVector * 0.5;
                    float2 reflectionUVs = reflectionVector.xy * _ReflectionTex_ST.xy;
                    reflectionUVs += _ReflectionTex_ST.zw * (_Time.y + input.customData.z);
                    
                    float3 reflectionTex = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUVs).rgb;

                    // Generate specular
                    float desatReflection = dot(reflectionTex, float3(1, 1, 1)) * 0.333;
                    float3 spec = lerp(desatReflection, reflectionTex, _ReflectionSat);
                    float3 spec0 = spec;
                    float3 spec1 = spec0 * spec0 * spec0 * spec0;
                    spec = clamp(lerp(spec0, spec1, _SpecularColor.w * preClipAlpha), 0, 10);

                    float fresnel = 1 - dot(normalize(input.viewDirWS), combinedNormals) * _SpecularColor.w;
                    spec *= clamp(fresnel, 0.2, 1);
                #endif

                // Find edge
                half edge = 1 - saturate(preClipAlpha * clippedAlpha);
                edge *= edge;
                edge = 1 - edge;
                edge = edge + lerp(0, noise - 0.5, _NoiseColorStr);

                // Edge darken
                edge = saturate(lerp(0.71, edge * edge, _EdgeDarken));

                // Edge alpha
                col.a *= saturate(lerp(1.25, _BaseColor.a, edge));

                #ifndef SPECULAR_REFLECTION_ON
                    edge *= 2;
                #endif

                col.xyz *= lerp(min(col.xyz * col.xyz * col.xyz * 0.3, 1.0), 0.71, edge);

                // Tint and combine lighting
                col.xyz *= max(0, baseLighting * _BaseColor.xyz);

                #ifdef SPECULAR_REFLECTION_ON
                    col.xyz += baseLighting * spec * _SpecularColor.xyz;
                #endif

                // Apply fog
                col.rgb = MixFog(col.rgb, input.fogFactor);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
