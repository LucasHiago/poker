const vec2 positions[] = vec2[]
(
	vec2(-1, -1),
	vec2(-1,  3),
	vec2( 3, -1)
);

layout(location=0) noperspective out vec2 screenCoord_out;

void main()
{
	gl_Position = vec4(positions[gl_VertexID], 0, 1);
	screenCoord_out = (gl_Position.xy / gl_Position.w) * 0.5 + 0.5;
}
