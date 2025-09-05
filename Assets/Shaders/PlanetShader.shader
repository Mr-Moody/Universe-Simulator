Shader "Custom/PlanetShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 color : COLOR;
            };

            fixed4 _BaseColor;
            float _Metallic;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);

                // Boost vertex color and apply base color
                o.color.rgb = v.color.rgb * _BaseColor.rgb * 1.2; // slightly brighter
                o.color.a = v.color.a;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Normalize normal
                float3 N = normalize(i.normal);

                // Light vector (directional vs positional)
                float3 L = (_WorldSpaceLightPos0.w == 0.0) ?
                            normalize(_WorldSpaceLightPos0.xyz) :
                            normalize(_WorldSpaceLightPos0.xyz - i.worldPos);

                // View and half-vector
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 H = normalize(L + V);

                // Lambert diffuse, slightly brighter
                float NdotL = saturate(dot(N, L));
                float3 diffuse = i.color.rgb * NdotL; // full intensity (not dimmed)

                // Optional: slightly boost saturation
                float avg = (diffuse.r + diffuse.g + diffuse.b) / 3.0;
                diffuse = lerp(avg.xxx, diffuse, 1.2); // 20% more saturated

                // Softened Phong specular
                float smoothness = 1.0 - pow(i.color.a, 0.5);
                float NdotH = saturate(dot(N, H));
                float specular = pow(NdotH, smoothness * 16.0);
                float3 spec = specular * lerp(0.04, i.color.rgb, _Metallic) * 0.1;
                spec *= NdotL; // prevent spec on shadowed sides

                // Fresnel for oceans only
                float fresnelPower = 5.0;
                float fresnel = pow(1.0 - saturate(dot(V, N)), fresnelPower);
                fresnel *= NdotL; // only lit surfaces
                float oceanMask = smoothstep(0.0, 0.5, i.color.b); // blue = water
                fresnel *= 0.08 * oceanMask; // subtle highlight

                // Blend: add specular + Fresnel on top of diffuse (do not dim diffuse)
                float3 finalColor = diffuse + spec + fresnel;

                // Clamp and gamma correct (subtle gamma)
                finalColor = saturate(finalColor);
                finalColor = pow(finalColor, 1.0/1.8); // preserves vibrancy

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
