#version 400
uniform mat4 World;
uniform mat4 ViewProjection;

in vec3 position;
in vec3 normal;
in vec2 textureCoordinate;

out vec3 psNormal;
out vec2 psTexcoord;

void main()
{
	psNormal = normal;
	psTexcoord = textureCoordinate;

	gl_Position = (ViewProjection * World) * vec4(position, 1.0);
}
