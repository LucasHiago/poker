layout(location=0) in vec3 position_in;

layout(binding=0, std140) uniform ShadowMatrixUB
{
	mat4 shadowMatrix;
};

void main()
{
	gl_Position = shadowMatrix * vec4(position_in, 1.0);
}
