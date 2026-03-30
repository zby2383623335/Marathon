Shader "UI/SoftenEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurAmount;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurAmount;

                // Original color
                fixed4 original = tex2D(_MainTex, i.uv);

                // Gaussian blur with boundary checking (inward-only blur)
                fixed4 blurred = fixed4(0, 0, 0, 0);
                float totalWeight = 0.0;

                // Center pixel
                blurred += original * 6.0;
                totalWeight += 6.0;

                // Right
                if (i.uv.x + texelSize.x <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(texelSize.x, 0)) * 3.0;
                    totalWeight += 3.0;
                }

                // Left
                if (i.uv.x - texelSize.x >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv - float2(texelSize.x, 0)) * 3.0;
                    totalWeight += 3.0;
                }

                // Top
                if (i.uv.y + texelSize.y <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(0, texelSize.y)) * 3.0;
                    totalWeight += 3.0;
                }

                // Bottom
                if (i.uv.y - texelSize.y >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv - float2(0, texelSize.y)) * 3.0;
                    totalWeight += 3.0;
                }

                // Right 2x
                if (i.uv.x + texelSize.x * 2.0 <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(texelSize.x * 2.0, 0)) * 1.0;
                    totalWeight += 1.0;
                }

                // Left 2x
                if (i.uv.x - texelSize.x * 2.0 >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv - float2(texelSize.x * 2.0, 0)) * 1.0;
                    totalWeight += 1.0;
                }

                // Top 2x
                if (i.uv.y + texelSize.y * 2.0 <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(0, texelSize.y * 2.0)) * 1.0;
                    totalWeight += 1.0;
                }

                // Bottom 2x
                if (i.uv.y - texelSize.y * 2.0 >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv - float2(0, texelSize.y * 2.0)) * 1.0;
                    totalWeight += 1.0;
                }

                // Top-Left
                if (i.uv.x - texelSize.x >= 0.0 && i.uv.y + texelSize.y <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(-texelSize.x, texelSize.y)) * 1.5;
                    totalWeight += 1.5;
                }

                // Top-Right
                if (i.uv.x + texelSize.x <= 1.0 && i.uv.y + texelSize.y <= 1.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(texelSize.x, texelSize.y)) * 1.5;
                    totalWeight += 1.5;
                }

                // Bottom-Left
                if (i.uv.x - texelSize.x >= 0.0 && i.uv.y - texelSize.y >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(-texelSize.x, -texelSize.y)) * 1.5;
                    totalWeight += 1.5;
                }

                // Bottom-Right
                if (i.uv.x + texelSize.x <= 1.0 && i.uv.y - texelSize.y >= 0.0) {
                    blurred += tex2D(_MainTex, i.uv + float2(texelSize.x, -texelSize.y)) * 1.5;
                    totalWeight += 1.5;
                }

                // Normalize by actual weight used
                blurred /= totalWeight;

                // Calculate distance to edge for fade-out effect
                float distToEdge = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float fadeDistance = 0.1;
                float edgeFade = smoothstep(0.0, fadeDistance, distToEdge);

                // Apply fade to alpha
                blurred.a *= edgeFade;

                // Apply UI Image Color
                blurred *= i.color;

                return blurred;
            }
            ENDCG
        }
    }
}
