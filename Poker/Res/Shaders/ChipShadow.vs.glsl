layout(location=0) in vec3 position_in;

layout(binding=0, std140) readonly buffer PositionBuffer
{
	vec4 transforms[];
};

layout(binding=0, std140) uniform ShadowMatrixUB
{
	mat4 shadowMatrix;
};

uniform float scale;

void main()
{
	vec3 worldPos = position_in * scale + transforms[gl_InstanceID].xyz;
	
	gl_Position = shadowMatrix * vec4(worldPos, 1.0);
}
