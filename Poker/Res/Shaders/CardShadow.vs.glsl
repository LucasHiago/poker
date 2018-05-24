layout(location=0) in vec2 position_in;

layout(location=0) out vec2 texCoord_out;

uniform mat4 worldTransform;

layout(binding=0, std140) uniform ShadowMatrixUB
{
	mat4 shadowMatrix;
};

void main()
{
	texCoord_out = (position_in + 1.0) / 2.0;
	texCoord_out.y = 1.0 - texCoord_out.y;
	
	vec3 worldPos = (worldTransform * vec4(position_in.x, 0.0, position_in.y, 1.0)).xyz;
	gl_Position = shadowMatrix * vec4(worldPos, 1.0);
}