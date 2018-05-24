#include "Lighting.glh"

layout(location=0) in vec3 normal_in;
layout(location=1) in vec3 worldPos_in;

layout(location=0) out vec4 color_out;

uniform vec3 albedo;
uniform float specularExponent;
uniform float specularIntensity;

void main()
{
	vec3 lighting = calcLighting(albedo, normalize(normal_in), worldPos_in, specularIntensity, specularExponent);
	
	color_out = vec4(pow(lighting, vec3(1.0 / 2.2)), 1.0);
}
