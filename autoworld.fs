#version 440
out vec4 finalColor; // output fragment color

uniform vec2 resolution; // screen size
uniform float z;
uniform vec2 offset;
uniform float zoom;
uniform float brightness = 1.0;
uniform float time;

// ==================== Simplex Noise ====================
// https://github.com/ashima/webgl-noise/blob/master/src/noise4D.glsl

vec3  mod7(vec3 x)   { return x - floor(x * (1.0 / 7.0)) * 7.0;}
vec4  mod7(vec4 x)   { return x - floor(x * (1.0 / 7.0)) * 7.0;}
vec2  mod289(vec2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec3  mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec4  mod289(vec4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float mod289(float x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

dvec2  mod289(dvec2  x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
dvec3  mod289(dvec3  x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
dvec4  mod289(dvec4  x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
double mod289(double x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
dvec3  permute(dvec3  x) { return mod289(((x*34.0)+1.0)*x); }
dvec4  permute(dvec4  x) { return mod289(((x*34.0)+1.0)*x); }
double permute(double x) { return mod289(((x*34.0)+1.0)*x); }

vec3  permute(vec3 x) { return mod289(((x*34.0)+1.0)*x); }
vec4  permute(vec4 x) { return mod289(((x*34.0)+1.0)*x); }
float permute(float x) { return mod289(((x*34.0)+1.0)*x); }
vec4  taylorInvSqrt(vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }
float taylorInvSqrt(float r) { return 1.79284291400159 - 0.85373472095314 * r; }

float snoise(vec3 v) {
  const vec2  C = vec2(1.0/6.0, 1.0/3.0) ;
  const vec4  D = vec4(0.0, 0.5, 1.0, 2.0);

// First corner
  vec3 i  = floor(v + dot(v, C.yyy) );
  vec3 x0 =   v - i + dot(i, C.xxx) ;

// Other corners
  vec3 g = step(x0.yzx, x0.xyz);
  vec3 l = 1.0 - g;
  vec3 i1 = min( g.xyz, l.zxy );
  vec3 i2 = max( g.xyz, l.zxy );

  //   x0 = x0 - 0.0 + 0.0 * C.xxx;
  //   x1 = x0 - i1  + 1.0 * C.xxx;
  //   x2 = x0 - i2  + 2.0 * C.xxx;
  //   x3 = x0 - 1.0 + 3.0 * C.xxx;
  vec3 x1 = x0 - i1 + C.xxx;
  vec3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
  vec3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

// Permutations
  i = mod289(i);
  vec4 p = permute( permute( permute(
                               i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
                             + i.y + vec4(0.0, i1.y, i2.y, 1.0 ))
                    + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

// Gradients: 7x7 points over a square, mapped onto an octahedron.
// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
  float n_ = 0.142857142857; // 1.0/7.0
  vec3  ns = n_ * D.wyz - D.xzx;

  vec4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

  vec4 x = x_ *ns.x + ns.yyyy;
  vec4 y = y_ *ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);

  vec4 b0 = vec4( x.xy, y.xy );
  vec4 b1 = vec4( x.zw, y.zw );

  //vec4 s0 = vec4(lessThan(b0,0.0))*2.0 - 1.0;
  //vec4 s1 = vec4(lessThan(b1,0.0))*2.0 - 1.0;
  vec4 s0 = floor(b0)*2.0 + 1.0;
  vec4 s1 = floor(b1)*2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));

  vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
  vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

  vec3 p0 = vec3(a0.xy,h.x);
  vec3 p1 = vec3(a0.zw,h.y);
  vec3 p2 = vec3(a1.xy,h.z);
  vec3 p3 = vec3(a1.zw,h.w);

//Normalise gradients
  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;

// Mix final noise value
  vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
  m = m * m;
  return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1),
                                dot(p2,x2), dot(p3,x3) ) );
}

// ==================== main ====================

// screen => world coordinates
dvec2 screen2world(vec2 fragCoord) {
  return ((fragCoord - resolution/2) * zoom) + offset;
}

double height0(dvec2 start, double z, int octave) {
  const float persistence = 1.5;
  const float lacunarity = 2.5;
  double factor = (1/pow(persistence, octave));
  return factor * snoise(vec3(start * pow(lacunarity, octave), z));
}

double height(dvec2 start, double z) {
  const float persistence = 0.71;
  const float lacunarity = 2.3;
  int octave_start = -20;
  int octave_end = 0;
  double sum = 0;
  double amplitude  = pow(persistence, octave_start);
  double wavelength = pow(lacunarity,  octave_start);
  double amplitude_max = 0;
  for(int octave = octave_start ; octave < octave_end; octave++) {
    sum += amplitude * snoise(vec3(start * wavelength, z + octave/*or another hash*/));
    amplitude_max += amplitude;
    wavelength *= lacunarity;
    amplitude *= persistence;
  }
  return sum / amplitude_max;
}

bool box(double h) {
  return h > 0;
}

void main() {
  dvec2 p = screen2world(gl_FragCoord.xy);
  p -= mod(p, 1);
  double h = height(p, z);
  const vec3 ground = vec3(0.1);
  const vec3 frozen = vec3(0.2);
  const vec3 free   = vec3(0.3);
  vec3 color = box(h) ? free : ground;
  //double b = box(height(p - mod(p, 1), z)) ? .3 : 0.0;
  //vec3 color = vec3((-h), (h), b);
  /**/ if(box(h)
          && box(height(p + vec2( 1, 0), z))
          && box(height(p + vec2( 0, 1), z))
          && box(height(p + vec2( 1, 1), z))) color = frozen;
  else if(box(h)
          && box(height(p + vec2(-1, 0), z))
          && box(height(p + vec2( 0,-1), z))
          && box(height(p + vec2(-1,-1), z))) color = frozen;
  else if(box(h)
          && box(height(p + vec2( 1, 0), z))
          && box(height(p + vec2( 0,-1), z))
          && box(height(p + vec2( 1,-1), z))) color = frozen;
  else if(box(h)
          && box(height(p + vec2(-1, 0), z))
          && box(height(p + vec2( 0, 1), z))
          && box(height(p + vec2(-1, 1), z))) color = frozen;
  
  if(zoom < 0.2 && floor(screen2world(gl_FragCoord.xy + 0.5))
     /**/        != floor(screen2world(gl_FragCoord.xy - 0.5)))
    color *= 0.6;
  finalColor = vec4(color, 1);
}
