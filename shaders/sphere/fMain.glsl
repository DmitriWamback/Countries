#version 410 core

out vec4 fragc;

in vec3 normal;
uniform vec3 cameraPosition;
in vec2 uv;

uniform sampler2D surfaceTexture;
uniform float time;

void main() {

    //fragc = vec4(uv, 0.0, 1.0);
    vec3 cities = texture(surfaceTexture, uv).rgb;
    float average = 0.2126 * cities.r + 0.7152 * cities.g + 0.0722 * cities.b;
    vec3 color = average > 0.5 ? vec3(average * 0.9, average * 0.8, 0.0) : vec3(0.1);
    color = average < time ? vec3(0.05) : color;

    fragc = vec4(vec3(average), 1.0);
}