#include <stdio.h>
#include <sys/stat.h>
#include "raylib.h"

#define BADSHADER 3 // HACK! 3 is built-in shader. how to detect shader fails?

// A few good julia sets
const float POINTS_OF_INTEREST[6][2] =
{
  { 0, 8.836337089539 },
  { -0.786268, 0.169728 },
  { -0.8, 0.156 },
  {-0.823356747627,-0.216459855437} , // { 0.285, 0.0 },
  { -0.835, -0.2321 },
  { -0.70176, -0.3842 },
  // { -0.831522226334,-0.238089501858 },
};

int main(void)
{
  SetConfigFlags(FLAG_WINDOW_RESIZABLE);
  InitWindow(900, 600, "raylib [shaders] example - julia sets [floating]");

  int idle = 0;

  char shaderPath[] = "autoworld.fs";
  Shader shader = LoadShader(0, shaderPath);

  float c[2] = { POINTS_OF_INTEREST[0][0], POINTS_OF_INTEREST[0][1] };

  float offsetStart[2] = {0,0};//{0.492528051138,0.150938585401};
  float offset[2] = {offsetStart[0], offsetStart[1]};
  float zoomStart = 1.0;
  float zoom = zoomStart;
  float brightness = 1.0f;

  Vector2 offsetSpeed = { 0.0f, 0.0f };

  SetTargetFPS(60);               // Set our game to run at 60 frames-per-second

  struct stat shaderPathStat = {0};
  stat(shaderPath, &shaderPathStat);
  time_t shaderPath_mtime = shaderPathStat.st_mtim.tv_sec;
  
  unsigned long frame = 0;
  Vector2 mstart = {0};
  // Main game loop
  while (!WindowShouldClose()) {
    int dirty = 0;
    if(IsWindowResized()) dirty = 1;
    if(frame == 0 || (frame % 10 == 0)) {
      stat(shaderPath, &shaderPathStat);
      if(frame==0 || (shaderPathStat.st_mtim.tv_sec != shaderPath_mtime)) {
        printf("refreshing shader %s\n", shaderPath);
        if(shader.id != BADSHADER)
          UnloadShader(shader);
        shader = LoadShader(0, shaderPath);
        dirty = 1;
        shaderPath_mtime = shaderPathStat.st_mtim.tv_sec;
      }
    }

    if(IsKeyPressed(KEY_ZERO)) c[0] = c[1], dirty = 1;
    if(IsKeyPressed(KEY_P)) { idle = !idle; }
    if(IsKeyPressed(KEY_SPACE)) {
      offset[0] = offsetStart[0];
      offset[1] = offsetStart[1];
      if(IsKeyDown(KEY_LEFT_SHIFT))
        zoom = zoomStart;
      dirty = 1;
    }
    if(0) ;
    else if (IsKeyPressed(KEY_ONE))   c[0] = POINTS_OF_INTEREST[0][0], c[1] = POINTS_OF_INTEREST[0][1], dirty=1;
    else if (IsKeyPressed(KEY_TWO))   c[0] = POINTS_OF_INTEREST[1][0], c[1] = POINTS_OF_INTEREST[1][1], dirty=1;
    else if (IsKeyPressed(KEY_THREE)) c[0] = POINTS_OF_INTEREST[2][0], c[1] = POINTS_OF_INTEREST[2][1], dirty=1;
    else if (IsKeyPressed(KEY_FOUR))  c[0] = POINTS_OF_INTEREST[3][0], c[1] = POINTS_OF_INTEREST[3][1], dirty=1;
    else if (IsKeyPressed(KEY_FIVE))  c[0] = POINTS_OF_INTEREST[4][0], c[1] = POINTS_OF_INTEREST[4][1], dirty=1;
    else if (IsKeyPressed(KEY_SIX))   c[0] = POINTS_OF_INTEREST[5][0], c[1] = POINTS_OF_INTEREST[5][1], dirty=1;

    if(GetMouseWheelMove() != 0) {
      zoom += GetMouseWheelMove() * zoom*0.3f;
      dirty = 1;
    }
    if(IsMouseButtonPressed(MOUSE_LEFT_BUTTON)) {
      mstart = GetMousePosition();
    }
    if (IsMouseButtonDown(MOUSE_LEFT_BUTTON)) {
      Vector2 mp = GetMousePosition();
      Vector2 delta = { mp.x - mstart.x , mp.y - mstart.y };
      offset[0] += 2.0/GetScreenWidth()  * -delta.x / zoom;
      offset[1] += 2.0/GetScreenHeight() *  delta.y / zoom;
      dirty = 1;
      mstart.x = mp.x;
      mstart.y = mp.y;
    }
    if(IsKeyDown(KEY_PERIOD)) brightness += 0.01f, dirty = 1;
    if(IsKeyDown(KEY_COMMA)) brightness -= 0.01f, dirty = 1;
    if(IsKeyDown(KEY_SLASH)) brightness  = 1.0f,   dirty = 1;
    if(brightness < 0.0f) brightness = 0.0f;

    float amount1 = 0, amount2 = 0;
    if(IsKeyDown(KEY_W) || IsKeyDown(KEY_UP   )) amount1 =  10.0f;
    if(IsKeyDown(KEY_S) || IsKeyDown(KEY_DOWN )) amount1 = -10.0f;
    if(IsKeyDown(KEY_A) || IsKeyDown(KEY_LEFT )) amount2 =  10.0f;
    if(IsKeyDown(KEY_D) || IsKeyDown(KEY_RIGHT)) amount2 = -10.0f;
    if(IsKeyDown(KEY_LEFT_CONTROL)) amount1 *=  10.0f, amount2 *=  10.0f;
    if(IsKeyDown(KEY_LEFT_SHIFT))   amount1 *=  20.0f, amount2 *=  20.0f;
    if(IsKeyDown(KEY_LEFT_ALT))     amount1 *=   0.1f, amount2 *=  0.1f;

    amount1 *= GetFrameTime() * 0.005f;
    amount2 *= GetFrameTime() * 0.005f;
    c[0] += amount2;
    c[1] += amount1;
    if(amount1 != 0.0f || amount2 != 0.0f && shader.id != BADSHADER)
      SetShaderValue(shader, GetShaderLocation(shader, "c"), c, UNIFORM_VEC2);

    if((dirty || frame==0) && shader.id != BADSHADER) {
      SetShaderValue(shader, GetShaderLocation(shader, "c"), c, UNIFORM_VEC2);
      SetShaderValue(shader, GetShaderLocation(shader, "zoom"), &zoom, UNIFORM_FLOAT);
      SetShaderValue(shader, GetShaderLocation(shader, "offset"), offset, UNIFORM_VEC2);
      // glUniform3dv(shader.id, GetShaderLocation(shader, "offset"), &offset);
      float resolution[2] = { (float)GetScreenWidth(), (float)GetScreenHeight() };
      SetShaderValue(shader, GetShaderLocation(shader, "resolution"), resolution, UNIFORM_VEC2);
      SetShaderValue(shader, GetShaderLocation(shader, "brightness"), &brightness, UNIFORM_FLOAT);
    }

    float time = GetTime();
    if(shader.id != BADSHADER && GetShaderLocation(shader, "time") >= 0)
      SetShaderValue(shader, GetShaderLocation(shader, "time"), &time, UNIFORM_FLOAT);

    BeginDrawing();
    if(dirty || !idle) {
    /**/BeginShaderMode(shader);
    /*  */DrawRectangle(0, 0, GetScreenWidth(), GetScreenHeight(), PURPLE);
    /**/EndShaderMode();

    }
    /**/Color color_text = MAGENTA; int size=20; int ty = 10;
    /**/DrawText(FormatText("fps %d @ %.1f", GetFPS(), time), 10, ty, size, color_text); ty+=size;
    /**/DrawText(FormatText("offset [%.4f,%.4f]",
                            offset[0], offset[1]), 10, ty, size, color_text); ty+=size;
    /**/DrawText(FormatText("zoom %.3f (%.0f²)", zoom, 2.0/(zoom)), 10, ty, size, color_text); ty+=size;
    /**/DrawText(FormatText("c [%.6f,%.6f]", c[0], c[1]), 10, ty, size, color_text); ty+=size;
    ///**/DrawText(FormatText("brightness %.3f", brightness), 10, ty, size, color_text); ty+=size;
    EndDrawing();

    frame++;
  }

  printf("c {%.12f,%.12f}\n", c[0], c[1]);
  printf("o {%.12f,%.12f}\n", offset[0], offset[1]);
  UnloadShader(shader);
  CloseWindow();

  return 0;
}
