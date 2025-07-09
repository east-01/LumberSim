Shader "UI/DashedImage"
{
    Properties
    {
        _MainTex       ("Texture",       2D)    = "white" {}
        _Color         ("Tint",          Color) = (1,1,1,1)
        _DashColor     ("Dash Color",    Color) = (1,0,0,1)
        _DashThickness ("Dash Thickness (px)", Float) = 2
        _DashLength    ("Dash Length (px)",    Float) = 10
        _DashSpacing   ("Dash Spacing (px)",   Float) = 5
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanvasType"="Overlay"
        }
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 position  : SV_POSITION;
                float4 screenPos : TEXCOORD1;    // NEW
                fixed4 color     : COLOR;
                float2 uv        : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4   _Color;
            float4   _DashColor;
            float    _DashThickness;
            float    _DashLength;
            float    _DashSpacing;
            float2 _RotationData;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.position   = UnityObjectToClipPos(v.vertex);
                o.screenPos  = ComputeScreenPos(o.position);  // NEW: get clip-space for pixel conversion
                o.uv         = v.uv;
                o.color      = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // // THIS is your true pixel‐space coordinate inside the UI element:
                // float2 pixelPos = ClipPixelPos(i.screenPos);

                // // Dash cycle length in pixels
                // float total = _DashLength + _DashSpacing;
                // float inCycle = fmod(pixelPos.x, total);
                // bool isDash = inCycle < _DashLength;

                // // Vertical thickness in pixels
                // float vOff = fmod(pixelPos.y, _DashThickness);
                // bool inThick = vOff < _DashThickness;

                // // Sample your base texture
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // if (isDash && inThick)
                //     return _DashColor * baseCol.a;   // keep original alpha
                // else
                //     return baseCol;
                return baseCol;
            }
            ENDCG
        }
    }
}