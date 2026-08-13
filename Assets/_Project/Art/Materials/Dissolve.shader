// Shader tan biến dùng cho quái sau khi chạy xong animation gục xuống.
//
// Cách hoạt động: mỗi điểm ảnh được gán một giá trị nhiễu ngẫu nhiên; khi giá trị đó
// nhỏ hơn ngưỡng "_DissolveAmount" thì điểm ảnh bị vứt bỏ. Tăng dần ngưỡng từ 0 lên 1
// thì thân quái thủng dần rồi biến mất, thay vì đột ngột tắt.
//
// Một dải mỏng ngay quanh mép đang tan được tô sáng rực (_EdgeColor) — chi tiết này
// mới là thứ khiến hiệu ứng đọc được. Không có nó thì nhìn chỉ như model bị lỗi hiển thị.
//
// Viết bằng shader mặt phẳng (unlit) có nhận một nguồn sáng chính, cố tình giữ đơn giản
// để chạy nhẹ trên điện thoại và không phụ thuộc vào hệ chiếu sáng nào.
Shader "Survival/Dissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Màu nền", Color) = (1,1,1,1)

        _DissolveAmount ("Mức tan biến", Range(0,1)) = 0
        _NoiseScale ("Độ mịn của nhiễu", Range(1,60)) = 18
        _EdgeWidth ("Độ dày viền cháy", Range(0,0.3)) = 0.06
        [HDR] _EdgeColor ("Màu viền cháy", Color) = (1, 0.45, 0.1, 1)

        _AmbientBoost ("Độ sáng nền", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Back

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _DissolveAmount;
            float _NoiseScale;
            float _EdgeWidth;
            fixed4 _EdgeColor;
            float _AmbientBoost;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            // Hàm nhiễu giả ngẫu nhiên, đủ dùng và rất rẻ.
            // Không dùng texture nhiễu để không phải kèm thêm file ảnh vào project.
            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.71, 0.113, 0.419));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float valueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);   // làm mượt để nhiễu không bị vỡ hạt

                float n000 = hash31(i + float3(0,0,0));
                float n100 = hash31(i + float3(1,0,0));
                float n010 = hash31(i + float3(0,1,0));
                float n110 = hash31(i + float3(1,1,0));
                float n001 = hash31(i + float3(0,0,1));
                float n101 = hash31(i + float3(1,0,1));
                float n011 = hash31(i + float3(0,1,1));
                float n111 = hash31(i + float3(1,1,1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Nhiễu tính theo toạ độ THẾ GIỚI chứ không theo UV.
                // Lý do: model nhân vật dùng chung một bảng màu nhỏ với UV chồng lấn nhau,
                // nếu tính theo UV thì nhiều mảnh cơ thể sẽ tan biến cùng lúc y hệt nhau,
                // trông như bị lỗi thay vì như đang tan rã.
                float noise = valueNoise(i.worldPos * _NoiseScale);

                // Vứt bỏ điểm ảnh đã nằm dưới ngưỡng tan biến.
                clip(noise - _DissolveAmount);

                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;

                float3 normal = normalize(i.worldNormal);
                float ndotl = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                fixed3 lit = albedo.rgb * (_LightColor0.rgb * ndotl + _AmbientBoost);

                // Dải mỏng ngay sát mép đang tan thì tô sáng rực lên.
                float edge = 1.0 - saturate((noise - _DissolveAmount) / max(_EdgeWidth, 0.0001));
                lit = lerp(lit, _EdgeColor.rgb, edge * step(0.0001, _DissolveAmount));

                return fixed4(lit, 1);
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
