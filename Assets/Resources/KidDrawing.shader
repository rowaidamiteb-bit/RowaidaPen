// شكل رسم طفلة فوق الصورة الواقعية: خطوط رصاص حول الأشكال (ترجف شوي كأنها مرسومة باليد)،
// تظليل شمع مقلم بالأماكن الغامقة، وحبيبات ورق خفيفة. الأشياء البعيدة والسما ما ينرسم حولها خط
Shader "Rowaida/KidDrawing"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0.22, 0.16, 0.24, 1)
        _LineStrength ("Line Strength", Range(0, 1)) = 0.9
        _LineWidth ("Line Width (px)", Float) = 1.7
        _Wobble ("Hand Wobble (px)", Float) = 1.1
        _HatchStrength ("Crayon Hatch", Range(0, 1)) = 0.1
        _Grain ("Paper Grain", Range(0, 0.3)) = 0.05
        _Crayon ("Crayon Strokes", Range(0, 0.3)) = 0.09
        _FadeStart ("Line Fade Start", Float) = 55
        _FadeEnd ("Line Fade End", Float) = 140
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "KidDrawing"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            half4 _LineColor;
            float _LineStrength, _LineWidth, _Wobble, _HatchStrength, _Grain, _FadeStart, _FadeEnd, _Crayon;

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                float4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
                float2 px = 1.0 / _ScreenParams.xy;
                float2 sp = uv * _ScreenParams.xy;

                // رجفة اليد: الخط يتحرك شوي كل شوي (مثل الرسوم المتحركة المرسومة باليد)
                float boil = floor(_Time.y * 5.0);
                float2 wob = float2(Noise(sp / 38.0 + boil * 7.3), Noise(sp / 38.0 + 31.7 + boil * 3.1)) - 0.5;
                float2 suv = uv + wob * _Wobble * px * 2.0;

                float d0 = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float3 n0 = SampleSceneNormals(suv);
                float edge = 0.0;
                float2 offs[4] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1) };
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 o = offs[i] * px * _LineWidth;
                    float d = LinearEyeDepth(SampleSceneDepth(suv + o), _ZBufferParams);
                    float3 n = SampleSceneNormals(suv + o);
                    edge = max(edge, saturate((abs(d - d0) / max(d0, 0.01) - 0.05) * 10.0));
                    edge = max(edge, saturate((1.0 - dot(n, n0) - 0.3) * 2.5));
                }
                float far = saturate((d0 - _FadeStart) / max(_FadeEnd - _FadeStart, 0.01));
                edge *= 1.0 - far;
                // الخط مو متساوي: حبيبات رصاص
                edge *= 0.65 + 0.35 * Noise(sp / 2.2);

                // ضربات ألوان شمع مايلة على الأشكال (مو على السما)
                float onObject = 1.0 - far;
                float2 r = float2(sp.x * 0.8 + sp.y * 0.6, -sp.x * 0.6 + sp.y * 0.8);
                float stroke = Noise(r * float2(0.06, 0.9)) * 0.7 + Noise(r * float2(0.02, 0.35)) * 0.3;
                col.rgb *= 1.0 + (stroke - 0.5) * _Crayon * 2.0 * onObject;

                // تظليل شمع مقلم بالأماكن الغامقة (خفيف)
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float shade = saturate((0.42 - lum) * 2.2) * (1.0 - far);
                float hatch = step(0.6, frac((sp.x + sp.y) / 6.5 + Noise(sp / 26.0) * 0.7));
                col.rgb *= 1.0 - hatch * shade * _HatchStrength;

                // حبيبات ورق على كل الصورة
                float grain = Noise(sp / 1.6) * 0.6 + Noise(sp / 7.0) * 0.4;
                col.rgb *= 1.0 - _Grain * (grain - 0.5);

                half3 ink = col.rgb * 0.35 + _LineColor.rgb * 0.5;
                col.rgb = lerp(col.rgb, ink, saturate(edge * _LineStrength));
                return col;
            }
            ENDHLSL
        }
    }
}
