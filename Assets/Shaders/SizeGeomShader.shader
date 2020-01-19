Shader "Custom/SizeGeomShader" {
    Properties {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Size ("Size", float) = 1.0
    }

    SubShader {
        Pass {
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                half2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0;
                float3 worldPos : COLOR1;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Properties)
            UNITY_DEFINE_INSTANCED_PROP(float, _Size)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Properties)

            v2g vert(appdata v) {
                v2g o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0;
                o.normal = v.normal;
                o.diff.rgb += ShadeSH9(half4(worldNormal,1));
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                g2f o;
                UNITY_SETUP_INSTANCE_ID(IN[0]);
                UNITY_TRANSFER_INSTANCE_ID(IN[0], o);

                float scale = UNITY_ACCESS_INSTANCED_PROP(Properties, _Size);

                for (int i = 0; i < 3; i++) {
                    o.vertex = float4(IN[i].worldPos.xyz * scale, 0.0);
                    o.vertex = UnityObjectToClipPos(o.vertex);
                    o.uv = IN[i].uv;
                    o.diff = IN[i].diff;
                    triStream.Append(o);
                }

                triStream.RestartStrip();
            }

            float4 frag(g2f i) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 col = UNITY_ACCESS_INSTANCED_PROP(Properties, _Color);
                col *= i.diff;
                return col;
            }
            ENDCG
        }
    }
}
