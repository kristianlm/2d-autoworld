#version 440


out vec4 finalColor; // output fragment color

uniform vec2 screenDims; // screen size
uniform vec2 c; // c.x = real, c.y = imaginary component. Equation is z^2 + c
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

vec4 grad4(float j, vec4 ip) {
  const vec4 ones = vec4(1.0, 1.0, 1.0, -1.0);
  vec4 p,s;

  p.xyz = floor( fract (vec3(j) * ip.xyz) * 7.0) * ip.z - 1.0;
  p.w = 1.5 - dot(abs(p.xyz), ones.xyz);
  s = vec4(lessThan(p, vec4(0.0)));
  p.xyz = p.xyz + (s.xyz*2.0 - 1.0) * s.www; 

  return p;
}
						
// (sqrt(5) - 1)/4 = F4, used once below
#define F4 0.309016994374947451

double dsnoise(dvec2 v) {
  const dvec4 C = dvec4(0.211324865405187,  // (3.0-sqrt(3.0))/6.0
                        0.366025403784439,  // 0.5*(sqrt(3.0)-1.0)
                        -0.577350269189626,  // -1.0 + 2.0 * C.x
                        0.024390243902439); // 1.0 / 41.0
// First corner
  dvec2 i  = floor(v + dot(v, C.yy) );
  dvec2 x0 = v -   i + dot(i, C.xx);

// Other corners
  dvec2 i1;
  //i1.x = step( x0.y, x0.x ); // x0.x > x0.y ? 1.0 : 0.0
  //i1.y = 1.0 - i1.x;
  i1 = (x0.x > x0.y) ? dvec2(1.0, 0.0) : dvec2(0.0, 1.0);
  // x0 = x0 - 0.0 + 0.0 * C.xx ;
  // x1 = x0 - i1 + 1.0 * C.xx ;
  // x2 = x0 - 1.0 + 2.0 * C.xx ;
  dvec4 x12 = x0.xyxy + C.xxzz;
  x12.xy -= i1;

// Permutations
  i = mod289(i); // Avoid truncation effects in permutation
  dvec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
                     + i.x + dvec3(0.0, i1.x, 1.0 ));

  dvec3 m = max(0.5 - dvec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
  m = m*m ;
  m = m*m ;

// Gradients: 41 points uniformly over a line, mapped onto a diamond.
// The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)

  dvec3 x = 2.0 * fract(p * C.www) - 1.0;
  dvec3 h = abs(x) - 0.5;
  dvec3 ox = floor(x + 0.5);
  dvec3 a0 = x - ox;

// Normalise gradients implicitly by scaling m
// Approximation of: m *= inversesqrt( a0*a0 + h*h );
  m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );

// Compute final noise value at P
  dvec3 g;
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * x12.xz + h.yz * x12.yw;
  return 130.0 * dot(m, g);
}

float snoise(vec2 v) {
  const vec4 C = vec4(0.211324865405187,  // (3.0-sqrt(3.0))/6.0
                      0.366025403784439,  // 0.5*(sqrt(3.0)-1.0)
                      -0.577350269189626,  // -1.0 + 2.0 * C.x
                      0.024390243902439); // 1.0 / 41.0
// First corner
  vec2 i  = floor(v + dot(v, C.yy) );
  vec2 x0 = v -   i + dot(i, C.xx);

// Other corners
  vec2 i1;
  //i1.x = step( x0.y, x0.x ); // x0.x > x0.y ? 1.0 : 0.0
  //i1.y = 1.0 - i1.x;
  i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
  // x0 = x0 - 0.0 + 0.0 * C.xx ;
  // x1 = x0 - i1 + 1.0 * C.xx ;
  // x2 = x0 - 1.0 + 2.0 * C.xx ;
  vec4 x12 = x0.xyxy + C.xxzz;
  x12.xy -= i1;

// Permutations
  i = mod289(i); // Avoid truncation effects in permutation
  vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
                    + i.x + vec3(0.0, i1.x, 1.0 ));

  vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
  m = m*m ;
  m = m*m ;

// Gradients: 41 points uniformly over a line, mapped onto a diamond.
// The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)

  vec3 x = 2.0 * fract(p * C.www) - 1.0;
  vec3 h = abs(x) - 0.5;
  vec3 ox = floor(x + 0.5);
  vec3 a0 = x - ox;

// Normalise gradients implicitly by scaling m
// Approximation of: m *= inversesqrt( a0*a0 + h*h );
  m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );

// Compute final noise value at P
  vec3 g;
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * x12.xz + h.yz * x12.yw;
  return 130.0 * dot(m, g);
}

float snoise(vec3 v)
{ 
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

float snoise(vec4 v) {
  const vec4  C = vec4( 0.138196601125011,  // (5 - sqrt(5))/20  G4
                        0.276393202250021,  // 2 * G4
                        0.414589803375032,  // 3 * G4
                        -0.447213595499958); // -1 + 4 * G4

  vec4 i  = floor(v + dot(v, vec4(F4)) );
  vec4 x0 = v -   i + dot(i, C.xxxx);
  vec4 i0;
  vec3 isX = step( x0.yzw, x0.xxx );
  vec3 isYZ = step( x0.zww, x0.yyz );
  i0.x = isX.x + isX.y + isX.z;
  i0.yzw = 1.0 - isX;
  i0.y += isYZ.x + isYZ.y;
  i0.zw += 1.0 - isYZ.xy;
  i0.z += isYZ.z;
  i0.w += 1.0 - isYZ.z;

  vec4 i3 = clamp( i0, 0.0, 1.0 );
  vec4 i2 = clamp( i0-1.0, 0.0, 1.0 );
  vec4 i1 = clamp( i0-2.0, 0.0, 1.0 );

  vec4 x1 = x0 - i1 + C.xxxx;
  vec4 x2 = x0 - i2 + C.yyyy;
  vec4 x3 = x0 - i3 + C.zzzz;
  vec4 x4 = x0 + C.wwww;

  i = mod289(i); 
  float j0 = permute( permute( permute( permute(i.w) + i.z) + i.y) + i.x);
  vec4 j1 = permute( permute( permute( permute (
                                         i.w + vec4(i1.w, i2.w, i3.w, 1.0 ))
                                       + i.z + vec4(i1.z, i2.z, i3.z, 1.0 ))
                              + i.y + vec4(i1.y, i2.y, i3.y, 1.0 ))
                     + i.x + vec4(i1.x, i2.x, i3.x, 1.0 ));

  vec4 ip = vec4(1.0/294.0, 1.0/49.0, 1.0/7.0, 0.0) ;

  vec4 p0 = grad4(j0,   ip);
  vec4 p1 = grad4(j1.x, ip);
  vec4 p2 = grad4(j1.y, ip);
  vec4 p3 = grad4(j1.z, ip);
  vec4 p4 = grad4(j1.w, ip);

  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;
  p4 *= taylorInvSqrt(dot(p4,p4));

  vec3 m0 = max(0.6 - vec3(dot(x0,x0), dot(x1,x1), dot(x2,x2)), 0.0);
  vec2 m1 = max(0.6 - vec2(dot(x3,x3), dot(x4,x4)            ), 0.0);
  m0 = m0 * m0;
  m1 = m1 * m1;
  return 49.0 * ( dot(m0*m0, vec3( dot( p0, x0 ), dot( p1, x1 ), dot( p2, x2 )))
                  + dot(m1*m1, vec2( dot( p3, x3 ), dot( p4, x4 ) ) ) ) ;
}

// Convert Hue Saturation Value (HSV) color into RGB
vec3 Hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void grid(vec2 sp) {
  float increment = 0;
  float repeater = 1.0/zoom;
  if(mod(sp.x, repeater) <= repeater*.1) increment = .4;
  if(mod(sp.y, repeater) <= repeater*.1) increment = .4;
  // if(sp.x < .010 && sp.x > -.010) increment = .7;
  // if(sp.y < .010 && sp.y > -.010) increment = .7;
  finalColor.g += increment;
}

// ==================== main ====================

// screen => world coordinates
dvec2 point(vec2 fragCoord) {
  dvec2 aspect = dvec2(screenDims.x / screenDims.y, 1.0);
  return (((fragCoord / screenDims) - dvec2(0.5, 0.5)) * aspect / zoom) + offset;
}

double height0(dvec2 start, double z, int octave) {
  const float persistence = 1.5;
  const float lacunarity = 2.5;
  double factor = (1/pow(persistence, octave));
  return factor * snoise(vec3(start * pow(lacunarity, octave), z));
}

double height(vec2 fragCoord, double z) {
  dvec2 start = point(fragCoord);
  start -= mod(start, 0.0000001);
  double j = 0;
  for(int octave = 0 ; octave < 15; octave++) {
    j += height0(start, z, octave);
  }
  return j;
}

vec3 terrain(double height) {
  vec3 result;
  double z = height;

  const vec3 ocean = vec3(20/255.0, 0/255.0, 80/255.0);
  const vec3 deep =  vec3(11/255.0, 30/255.0, 120/255.0);
  const vec3 coast = vec3(31/255.0, 85/255.0, 187/255.0);
  const vec3 sand  = vec3(234/255.0, 213/255.0, 180/255.0);
  const vec3 grass = vec3(71/255.0, 133/255.0, 47/255.0);
  const vec3 forest = vec3(20/255.0, 83/255.0, 27/255.0);
  const vec3 highland = vec3(150/255.0, 150/255.0, 150/255.0);
  const vec3 mountain = vec3(200/255.0, 200/255.0, 200/255.0);

  //if(z < -1.8) result = deep;
  /**/ if(z < -0.3) result = ocean;
  else if(z < -0.1) result = deep;
  else if(z < 0) result = coast;
  else if(z < 0.002 && zoom > 500) result = sand;
  else if(z < 1) result = grass;
  else if(z < 1.7) result = forest;
  else if(z < 2.0) result = highland;
  else result = mountain;

  return result;
}

void main() {
  float psize = 0.1;
  double z = height(gl_FragCoord.xy + vec2(-psize,-psize), c.y);
  vec3 color = terrain(z);
  // color = mix(color, Hsv2rgb(vec3(z, 1, 1)), 0.0);
  
  if(false) {
    color += terrain(height(gl_FragCoord.xy + vec2(+psize,-psize), c.y));
    color += terrain(height(gl_FragCoord.xy + vec2(-psize,+psize), c.y));
    color += terrain(height(gl_FragCoord.xy + vec2(+psize,+psize), c.y));
    color /= 4;
  }

  dvec2 start = point(gl_FragCoord.xy + vec2(-psize,-psize));
  double z2 = 0;
  for(int octave = 0 ; octave < 15; octave++) {
    double factor = (1/pow(1.5, octave));
    double seed = octave == 14 ? c.x : c.y;
    z2 += factor * snoise(vec3(start * pow(2.5, octave), seed));
  }
  /* { */
  /*   int octave = 15; */
  /*   double factor = (1/pow(1.5, octave)); */
  /*   z2 += factor * snoise(vec3(start * pow(2.5, octave), c.y)); */
  /* } */
  /* if(z < 0) z = 0; */
  /* if(z > 1) z = 1; */
//  z2 = mix(z, z2, 0.5);
  //double z2 = height(gl_FragCoord.xy + vec2(-psize,-psize), c.x);
  //z2 = smoothstep(0, 1, z2);
  if(z2 > 0 && z2 < 0.002) z2 = 0.5;
  //z2 = smoothstep(0.002, 1, z2);
  //if(z2 > 1) z2 = 0.5;
  //if(z2 < 0.002) z2 = 0.5;
  //color = mix(color, Hsv2rgb(vec3(z2, 1, 1)), 0.5);
  finalColor = vec4(color, 1);
  if(false && gl_FragCoord.y / screenDims.y < 0.02)
    finalColor = vec4(Hsv2rgb(vec3(gl_FragCoord.x/screenDims.x, 1.0, 1.0)), 1);
}

