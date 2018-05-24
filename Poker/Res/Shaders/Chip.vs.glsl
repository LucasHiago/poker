#include "ViewProj.glh"

layout(location=0) in vec3 position_in;
layout(location=1) in vec3 normal_in;

layout(location=0) out vec3 normal_out;
layout(location=1) out vec3 worldPos_out;

layout(binding=0, std140) readonly buffer PositionBuffer
{
	vec4 transforms[];
};

vec3 rotateXZ(vec3 v, float sinR, float cosR)
{
	vec3 result;
	result.x = v.x * cosR - v.z * sinR;
	result.y = v.y;
	result.z = v.x * sinR + v.z * cosR;
	return result;
}

uniform float scale;

void main()
{
	float sinR = sin(transforms[gl_InstanceID].w);
	float cosR = cos(transforms[gl_InstanceID].w);
	
	worldPos_out = rotateXZ(position_in, sinR, cosR) * scale + transforms[gl_InstanceID].xyz;
	
	normal_out = rotateXZ(normal_in, sinR, cosR);
	
	gl_Position = viewProjTransform * vec4(worldPos_out, 1.0);
}
