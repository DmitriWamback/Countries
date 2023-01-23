#version 410 core

layout(location = 0) in vec3 vertex;

uniform mat4 projection;
uniform mat4 lookAt;
uniform mat4 rotation;

out vec3 normal;
out vec2 uv;

uniform float time;

float rad(float a) {
    return a;
}

vec2 pointOnSphereUV(vec3 point) {

    vec3 p = normalize(point);
    float longitude = atan(p.x, p.z) / (2 * 3.14159265358797) + 0.5 + 0.25;
    float latitude  = asin(p.y) / 3.14159265358797 + 0.5;

    float pi = 3.14159265358797;

    float u = longitude;
    float v = latitude;

    vec2 uv = vec2(u, 1 - v);
    return uv;
}

void main() {

    normal = normalize(vertex);

    vec2 outuv = pointOnSphereUV(vertex);
    uv = outuv;

    gl_Position = projection * lookAt * vec4(vertex, 1.0);
    gl_PointSize = 2;
}