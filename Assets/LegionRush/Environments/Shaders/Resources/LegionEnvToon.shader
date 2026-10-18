Shader "LegionRush/EnvToon"
{
    // Cenário estilizado: mesma luz em degrau do ToonLit dos personagens, mas com cor por vértice
    // (uma malha combinada serve dezenas de peças com um material só), emissão a partir do albedo
    // (chamas, cristais, janelas), balanço de vento por UV (estandartes) e neblina ajustável.
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Tint", Color) = (0.62,0.68,0.9,1)
        _RampThreshold ("Ramp Threshold", Range(-1,1)) = 0.05
        _RampSmoothness ("Ramp Smoothness", Range(0.001,1)) = 0.18
        _HighlightColor ("Highlight Color (A = intensidade)", Color) = (1,1,1,0.12)
        _HighlightSize ("Highlight Size", Range(0,1)) = 0.35
        _RimColor ("Rim Color", Color) = (0.85,0.93,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _RimIntensity ("Rim Intensity", Range(0,2)) = 0.25
        _EmissionStrength ("Emission (x albedo)", Range(0,8)) = 0
        _FogScale ("Fog Scale", Range(0,1)) = 1
        _WaveAmount ("Wave Amount (m)", Range(0,1)) = 0
        _WaveSpeed ("Wave Speed", Range(0,10)) = 3
        _WaveFrequency ("Wave Frequency", Range(0,10)) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _RampThreshold;
            half _RampSmoothness;
            half4 _HighlightColor;
            half _HighlightSize;
            half4 _RimColor;
            half _RimPower;
            half _RimIntensity;
            half _EmissionStrength;
            half _FogScale;
            float _WaveAmount;
            float _WaveSpeed;
            float _WaveFrequency;
        CBUFFER_END

        half4 _LR_ShadowTint;
        half _LR_ShadowTintAmount;
        half _LR_EmissionBoost;

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Estandartes: o tecido balança conforme o peso no alfa do vértice (0 preso no mastro, 1 ponta livre).
        // Em espaço de mundo para continuar correto depois de combinar as malhas.
        float3 ApplyWave(float3 positionWS, half weight)
        {
            float w = _WaveAmount * weight;
            float phase = _Time.y * _WaveSpeed + positionWS.x * 0.37 + positionWS.z * 0.21 - weight * _WaveFrequency;
            positionWS.x += sin(phase) * w;
            positionWS.z += cos(phase * 0.8) * w * 0.6;
            positionWS.y += sin(phase * 1.3) * w * 0.15;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWave(positionWS, input.color.a);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb * input.color.rgb;
                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = dot(normalWS, mainLight.direction);
                half ramp = smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, ndl);
                ramp *= lerp(1.0h, mainLight.shadowAttenuation, 0.85h);

                half3 ambient = SampleSH(normalWS);
                half occlusion = 1.0h;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                ambient *= ao.indirectAmbientOcclusion;
                occlusion = ao.directAmbientOcclusion;
            #endif

                half3 shade = _ShadeColor.rgb * lerp(half3(1.0h, 1.0h, 1.0h), _LR_ShadowTint.rgb, saturate(_LR_ShadowTintAmount));
                half3 diffuse = albedo * lerp(shade, half3(1.0h, 1.0h, 1.0h), ramp) * mainLight.color * occlusion;
                half3 halfDir = normalize(mainLight.direction + viewWS);
                half exponent = lerp(256.0h, 8.0h, _HighlightSize);
                half spec = smoothstep(0.45h, 0.55h, pow(saturate(dot(normalWS, halfDir)), exponent)) * ramp;
                half rim = pow(1.0h - saturate(dot(normalWS, viewWS)), _RimPower) * _RimIntensity;

                // Ambiente mais fraco que nos personagens: mantém contraste entre luz e sombra (neve não estoura).
                half3 color = diffuse + albedo * ambient * 0.5h;
                color += _HighlightColor.rgb * _HighlightColor.a * spec * mainLight.color;
                color += _RimColor.rgb * rim * (0.35h + 0.65h * ramp);
                color += albedo * _EmissionStrength * (1.0h + _LR_EmissionBoost);
                half3 fogged = MixFog(color, input.fogFactor);
                color = lerp(color, fogged, _FogScale);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings vertShadow(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = ApplyWave(TransformObjectToWorld(input.positionOS.xyz), input.color.a);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirection = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 fragShadow(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings vertDepth(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformWorldToHClip(ApplyWave(TransformObjectToWorld(input.positionOS.xyz), input.color.a));
                return output;
            }

            half fragDepth(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertNormals
            #pragma fragment fragNormals
            #pragma multi_compile_instancing

            struct NormalAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NormalVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            NormalVaryings vertNormals(NormalAttributes input)
            {
                NormalVaryings output = (NormalVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformWorldToHClip(ApplyWave(TransformObjectToWorld(input.positionOS.xyz), input.color.a));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 fragNormals(NormalVaryings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
