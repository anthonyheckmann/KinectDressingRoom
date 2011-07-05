Shader "Masked/Specular Masked" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		// This shader does the same thing as the Diffuse shader, but after masks
		// and before transparent things
		Tags {"Queue" = "Geometry+20" }
		Pass {}
		//UsePass "Specular/BASE"
		//UsePass "Specular/PPL"
	} 
	FallBack "Diffuse", 1
}
