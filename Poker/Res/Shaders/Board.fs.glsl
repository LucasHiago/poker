layout(location=0) in vec3 normal_in;
layout(location=1) in vec3 tangent_in;
layout(location=2) in vec2 texCoord_in;
layout(location=3) in vec3 worldPos_in;

layout(location=0) out vec4 color_out;

layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalMapSampler;
layout(binding=2) uniform sampler2D specularMapSampler;

#include "Lighting.glh"

uniform float specularExponent;
uniform float specularIntensity;

void main()
{
	vec3 normal = normalize(normal_in);
	vec3 tangent = normalize(tangent_in - dot(normal, tangent_in) * normal);
	
	mat3 tbnMatrix = mat3(tangent, cross(tangent, normal), normal);
	
	vec3 nmNormal = (texture(normalMapSampler, texCoord_in).rgb * (255.0 / 128.0)) - vec3(1.0);
	normal = normalize(tbnMatrix * nmNormal);
	
	vec3 color = texture(diffuseSampler, texCoord_in).rgb;
	
	float specIntensity = texture(specularMapSampler, texCoord_in).r * specularIntensity;
	vec3 lighting = calcLighting(color, normal, worldPos_in, specIntensity, specularExponent);
	
	color_out = vec4(pow(lighting, vec3(1.0 / 2.2)), 1.0);
}
