
Shader "Ballance/EarthAtmosphere"
{
    Properties
    {
        _Color ("Atmosphere Color", Color) = (0.22, 0.55, 1, 1)
        _Intensity ("Opacity", Range(0, 1)) = 0.65
        _RimPower ("Rim Sharpness", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Atmosphere"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _RimPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS =
                    TransformWorldToHClip(output.positionWS);

                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);

                float3 viewDirection =
                    normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                float rim = 1.0 -
                    saturate(dot(normal, viewDirection));

                rim = pow(rim, _RimPower);

                half4 result = _Color;
                result.a = saturate(rim * _Intensity);

                return result;
            }
            ENDHLSL
        }
    }
}
