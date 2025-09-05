Shader "Custom/AtmosphereShader"
{
    Properties
    {
        _AtmosphereColor("Atmosphere Color", Color) = (0.3,0.5,1,1) // blue
        _SunsetColor("Sunset Color", Color) = (1,0.4,0.2,1)        // red/orange
        _Intensity("Intensity", Range(0,1)) = 0.3
        _SunDir("Sun Direction", Vector) = (0,1,0,0)
        _PlanetRadius("Planet Radius", Float) = 1.0
        _Thickness("Atmosphere Thickness", Float) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend One One
        ZWrite Off

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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            fixed4 _AtmosphereColor;
            fixed4 _SunsetColor;
            float _Intensity;
            float4 _SunDir;
            float _PlanetRadius;
            float _Thickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 L = normalize(_SunDir.xyz);

                // Distance from planet surface for thickness
                float height = length(i.worldPos) - _PlanetRadius;
                float thicknessFactor = saturate(height / (_PlanetRadius * _Thickness));

                // Rim factor (camera-facing)
                float rim1 = pow(1.0 - saturate(dot(V, N)), 1.5);
                float rim2 = pow(1.0 - saturate(dot(V, N)), 3.0);
                float rim3 = pow(1.0 - saturate(dot(V, N)), 5.0);
                float rim = saturate(rim1 + rim2 * 0.5 + rim3 * 0.25);
                rim *= thicknessFactor;

                // Sun factor relative to camera
                float sunFacing = saturate(dot(L, V)); // 1 = looking at sun
                float sunsetFactor = pow(sunFacing, 2.0) * rim; // red at horizon opposite sun

                // Color blending: blue on sun side, red on horizon/opposite
                float3 color = lerp(_AtmosphereColor.rgb * rim, _SunsetColor.rgb * rim, sunsetFactor);

                // Inner volumetric glow
                float innerGlow = pow(saturate(dot(V, N)), 2.0);
                color += _AtmosphereColor.rgb * innerGlow * 0.05;

                // Apply overall intensity
                color *= _Intensity;

                return float4(color, rim * _Intensity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
