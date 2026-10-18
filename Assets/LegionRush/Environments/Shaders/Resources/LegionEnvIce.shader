Shader "LegionRush/EnvIce"
{
    // Gelo translúcido (cachoeiras congeladas, blocos de gelo): fresnel claro nas bordas, veios que escorrem
    // devagar (textura rolando em V) e brilho interno. Sem iluminação por pixel: barato no mobile.
    Properties
    {
        [MainTexture] _BaseMap ("Streaks", 2D) = "white" {}
        [MainColor] _BaseColor ("Deep Color", Color) = (0.25,0.6,1,0.7)
        _RimColor ("Rim Color", Color) = (0.85,0.97,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2
        _Glow ("Inner Glow", Range(0,4)) = 0.6
        _ScrollSpeed ("Scroll Speed", Range(-2,2)) = -0.05
        _Opacity ("Opacity", Range(0,1)) = 0.78
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Ice"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
                half _Glow;
                float _ScrollSpeed;
                half _Opacity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1.0h - saturate(abs(dot(normalWS, viewWS))), _RimPower);
                float2 uv = input.uv + float2(0.0, _Time.y * _ScrollSpeed);
                half streak = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).r;
                half streak2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * float2(0.5, 0.35) + float2(0.37, 0.0)).r;
                half sky = saturate(normalWS.y * 0.5h + 0.5h);

                half3 deep = _BaseColor.rgb * input.color.rgb;
                half3 color = deep * (0.75h + 0.5h * sky);
                color = lerp(color, _RimColor.rgb, saturate(fresnel * 0.9h + streak * 0.45h));
                color += deep * _Glow * (0.35h + streak2 * 0.65h);
                half alpha = saturate(_Opacity * (0.7h + 0.3h * fresnel + 0.2h * streak)) * _BaseColor.a * input.color.a;
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
