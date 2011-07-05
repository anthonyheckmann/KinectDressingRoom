Shader "Masked/Diffuse Masked" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		// This shader does the same thing as the Diffuse shader, but after masks
		// and before transparent things
		Tags {"Queue" = "Geometry+20" }
		Pass {
		    Material {
				Diffuse [_Color]
				Ambient [_Color]
				Shininess [_Shininess]
				Specular [_SpecColor]
				Emission [_Emission]
			}
			Lighting On
			SeparateSpecular On
			SetTexture [_MainTex] {
			constantColor [_Color]
			Combine texture * primary DOUBLE, texture * constant
		}
	}
		//UsePass "Diffuse/BASE"
		//UsePass "Diffuse/PPL"
	} 
	FallBack "Diffuse", 1
}
