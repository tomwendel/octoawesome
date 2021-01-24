#version 400
uniform mat4 DepthWorldViewProj;

in uint inputData1;
in uint inputData2;


void main()
{
	vec4 position = vec4((inputData1 & 0xFF),((inputData1 >> 8) & 0xFF),((inputData1 >> 16) & 0xFF),1.0);

	gl_Position = DepthWorldViewProj*position;
}
