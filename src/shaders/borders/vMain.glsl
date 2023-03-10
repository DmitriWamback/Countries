#version 410 core

layout(location = 0) in vec3 vertex;

uniform mat4 projection;
uniform mat4 lookAt;
uniform mat4 rotation;

out vec3 normal;
uniform float pointSize = 1;

void main() {

    normal = normalize(vertex);

    gl_Position = projection * lookAt * rotation * vec4(vertex, 1.0);
    gl_PointSize = pointSize;
}