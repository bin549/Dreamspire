shader_type canvas_item;

uniform vec2 center : source_pos = vec2(0.5, 0.5); // 血迹中心
uniform float radius : hint_range(0.0, 1.0) = 0.2; // 初始半径
uniform float spread_speed : hint_range(0.0, 2.0) = 0.3; // 扩散速度
uniform vec4 blood_color : source_color = vec4(0.8, 0.0, 0.0, 1.0); // 血液颜色
uniform sampler2D noise_tex : source_color; // 噪声纹理

void fragment() {
    vec2 uv = UV;
    float dist = distance(uv, center);

    // 动态扩散半径
    float animated_radius = radius + TIME * spread_speed;

    // 血迹基础渐变
    float base_mask = smoothstep(animated_radius, animated_radius - 0.05, dist);

    // 噪声细节
    vec2 noise_uv = uv * 3.0; // 缩放噪声
    float noise_val = texture(noise_tex, noise_uv).r * 0.3; // 控制边缘随机性

    float final_mask = clamp(base_mask - noise_val, 0.0, 1.0);

    COLOR = mix(vec4(0.0), blood_color, final_mask);
}