#version 450

layout (location = 0) in vec4 fsin_Color;

layout (location = 0) out vec4 fsout_Color;

layout (set = 0, binding = 0) uniform ModelMatrix
{
	mat4 model;
};

void main()
{
	fsout_Color = fsin_Color;
}
