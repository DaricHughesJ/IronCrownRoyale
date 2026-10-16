Shader "Custom/IronCrownLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.05
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertColor : COLOR;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            fixed4 _Color;
            fixed4 _BaseColor;
            half _Metallic;
            half _Smoothness;
            half4 _EmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertColor = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // Pseudo-random noise for subtle micro-surface detail
            float Hash3D(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 N = normalize(i.worldNormal);
                float3 L = normalize(float3(0.48, 0.82, -0.32));
                if ( any(_WorldSpaceLightPos0.xyz) )
                {
                    L = normalize(_WorldSpaceLightPos0.xyz);
                }

                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 H = normalize(L + V);

                // Base albedo selection
                float3 albedo = _Color.rgb;
                bool isTerrain = (dot(i.vertColor.rgb, float3(1,1,1)) > 0.01 && dot(i.vertColor.rgb, float3(1,1,1)) < 2.95);
                if (isTerrain)
                {
                    albedo = i.vertColor.rgb;
                    // Add subtle multi-frequency organic grass/dirt noise
                    float terrainNoise = sin(i.worldPos.x * 0.8) * cos(i.worldPos.z * 0.8) * 0.05
                                       + sin(i.worldPos.x * 2.2 + i.worldPos.z * 1.9) * 0.04;
                    albedo += terrainNoise;
                }
                else
                {
                    // Procedural stone masonry seams on vertical stone architecture
                    float verticality = 1.0 - abs(N.y);
                    if (verticality > 0.55 && _Metallic < 0.25 && _Smoothness < 0.45)
                    {
                        // Ashlar stone block seams
                        float courseY = frac(i.worldPos.y * 0.85);
                        float stagger = floor(i.worldPos.y * 0.85) * 0.5;
                        float blockX = frac((i.worldPos.x + i.worldPos.z + stagger) * 0.45);
                        float mortar = smoothstep(0.06, 0.12, courseY) * smoothstep(0.05, 0.10, blockX);
                        float stoneShade = lerp(0.82, 1.05, Hash3D(floor(float3((i.worldPos.x + i.worldPos.z + stagger) * 0.45, i.worldPos.y * 0.85, 0.0))));
                        albedo *= lerp(0.68, stoneShade, mortar);
                    }
                    else if (_Metallic > 0.5)
                    {
                        // Steel/brass specular micro-scratches
                        float scratch = sin(i.worldPos.x * 25.0 + i.worldPos.y * 25.0) * 0.03;
                        albedo += scratch;
                    }
                }

                // Dual-tone lighting: warm sun vs cool atmospheric sky ambient
                float ndotl = dot(N, L);
                float sunIntensity = smoothstep(-0.05, 0.45, ndotl);
                float skyBlend = N.y * 0.5 + 0.5;
                float3 ambientSky = float3(0.38, 0.44, 0.52);
                float3 ambientGround = float3(0.26, 0.23, 0.20);
                float3 ambient = lerp(ambientGround, ambientSky, skyBlend);
                float3 sunColor = float3(1.10, 0.98, 0.82);

                // Specular highlight
                float specPower = exp2(lerp(3.5, 9.5, _Smoothness));
                float spec = pow(saturate(dot(N, H)), specPower) * (_Smoothness * 0.75 + _Metallic * 0.85);

                // Atmospheric rim lighting for silhouette pop
                float vdotn = 1.0 - saturate(dot(N, V));
                float rim = pow(vdotn, 2.8) * 0.38;
                float3 rimColor = lerp(ambientSky, sunColor, saturate(ndotl + 0.2));

                float3 litColor = albedo * (ambient + sunColor * sunIntensity) + sunColor * spec + rimColor * rim + _EmissionColor.rgb;

                fixed4 finalCol = fixed4(litColor, 1.0);
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
