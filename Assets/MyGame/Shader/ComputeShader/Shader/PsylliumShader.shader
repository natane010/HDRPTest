Shader "TK/Custom/PsylliumShader"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        _Intensity("Intensity", Range(1, 10)) = 1

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend("Src Factor", Float) = 5  // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlend("Dst Factor", Float) = 10 // OneMinusSrcAlpha

        [HideInInspector] _Mode("__mode", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        //Blend SrcAlpha One
        //Blend DstAlpha OneMinusDstAlpha
         Blend[_SrcBlend][_DstBlend]
        Fog
        {
            Mode Off
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct PsylliumData
            {
                // 座標
                float3 Position;
                // 回転
                float2x2 Rotation;
                // 色
                float4 PsylliumColor;
            };

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<PsylliumData> _PsylliumDataBuffer;
            float3 _PsylliumMeshScale;
            float3 _Color;
            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Intensity;
            CBUFFER_END

            float random(float2 st, int seed)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233)) + seed) * 43758.5453123);
            }

            v2f vert(appdata_t v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.color = _PsylliumDataBuffer[instanceID].PsylliumColor;


                float4x4 mat = unity_ObjectToWorld;
                float3 barUp = mat._m01_m11_m21;
                float3 barPos = mat._m03_m13_m23;

                // Y軸をロックして面をカメラに向ける姿勢行列を作る
                float3 cameraToBar = barPos - _WorldSpaceCameraPos;
                float3 barSide = normalize(cross(barUp, cameraToBar));
                float3 barForward = normalize(cross(barSide, barUp));

                mat._m00_m10_m20 = barSide;
                mat._m01_m11_m21 = barUp;
                mat._m02_m12_m22 = barForward;

                //// 回転を適用
                //v.vertex.y += ((_MainTex_TexelSize.w / 100) / 2);
                //float2x2 rotationMatrix = _PsylliumDataBuffer[instanceID].Rotation;
                //v.vertex.yz = mul(rotationMatrix, v.vertex.yz);
                //v.vertex.y -= ((_MainTex_TexelSize.w / 100) / 2);

                //// スケールと位置(平行移動)を適用
                //float4x4 matrix_ = (float4x4)0;
                //matrix_._11_22_33_44 = float4(_PsylliumMeshScale.xyz, 1.0);
                //matrix_._14_24_34 += _PsylliumDataBuffer[instanceID].Position;
                //v.vertex = mul(matrix_, v.vertex);

                float4 vertex = float4(v.vertex.xyz, 1.0);
                
                // 回転を適用
                vertex.y += ((_MainTex_TexelSize.w / 100) / 2);
                float2x2 rotationMatrix = _PsylliumDataBuffer[instanceID].Rotation;
                vertex.yz = mul(rotationMatrix, vertex.yz);
                vertex.y -= ((_MainTex_TexelSize.w / 100) / 2);

                // スケールと位置(平行移動)を適用
                mat._11_22_33_44 = float4(_PsylliumMeshScale.xyz, 1.0);
                mat._14_24_34 += _PsylliumDataBuffer[instanceID].Position;
                
                vertex = mul(mat, vertex);
                float3 offsetVec = normalize(cross(cameraToBar, barSide));
                vertex.xyz += offsetVec * v.uv2.x;

                //o.vertex = UnityObjectToClipPos(vertex);
                o.vertex = mul(UNITY_MATRIX_VP, vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                col.rgb =  i.color.rgb * _Intensity * _Color;
                clip(col.a - 0.1);
                //col.rgb *= clamp(random(i.uv, 1), 0, 1);
                col.rgb *= 6;
                col.a = 1.0;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
