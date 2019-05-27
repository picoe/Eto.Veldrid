#version 450

layout (location = 0) in vec3 Position;
layout (location = 1) in vec4 Color;

layout (location = 0) out vec4 fsin_Color;

layout (set = 0, binding = 0) uniform ViewMatrix
{
	mat4 view;
};

layout (set = 1, binding = 0) uniform ModelMatrix
{
	mat4 model;
};

void main()
{
	gl_Position = view * model * vec4(Position, 1);

	fsin_Color = Color;
}
