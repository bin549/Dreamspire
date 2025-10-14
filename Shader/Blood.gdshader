shader_type canvas_item;

uniform vec2 center : source_pos = vec2(0.5, 0.5); 
uniform float radius : hint_range(0.0, 1.0) = 0.2; 
uniform float spread_speed : hint_range(0.0, 2.0) = 0.3; 
uniform vec4 blood_color : source_color = vec4(0.8, 0.0, 0.0, 1.0); 
uniform sampler2D noise_tex : source_color; 

void fragment() {
    vec2 uv = UV;
    float dist = distance(uv, center);
    float animated_radius = radius + TIME * spread_speed;
    float base_mask = smoothstep(animated_radius, animated_radius - 0.05, dist);
    vec2 noise_uv = uv * 3.0; 
    float noise_val = texture(noise_tex, noise_uv).r * 0.3; 
    float final_mask = clamp(base_mask - noise_val, 0.0, 1.0);
    COLOR = mix(vec4(0.0), blood_color, final_mask);
}