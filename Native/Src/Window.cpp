#include "API.h"
#include "Utils.h"
#include "Graphics.h"
#include "stb_image.h"

#include <iostream>
#include <chrono>
#include <cstring>
#include <SDL2/SDL.h>
#include <SDL2/SDL_image.h>
#include <GL/glew.h>

constexpr int WIDTH = 1200;
constexpr int HEIGHT = 800;

#ifndef NDEBUG
void GLAPIENTRY OpenGLMessageCallback(GLenum, GLenum type, GLuint id, GLenum severity, GLsizei length,
                                      const GLchar* message, const void*)
{
	if (severity == GL_DEBUG_SEVERITY_NOTIFICATION)
		return;
	
	std::cout << "GL #" << id << ": " << message;
	if (message[length - 1] != '\n')
		std::cout << std::endl;
	else
		std::cout.flush();
	
	if (severity == GL_DEBUG_SEVERITY_HIGH)
		std::terminate();
}
#endif

extern int32_t MouseWheelX;
extern int32_t MouseWheelY;

using InitCallback = void(*)();
using CloseCallback = void(*)();
using FrameCallback = void(*)(float dt);
using ResizeCallback = void(*)(int width, int height);
using TextInputCallback = void(*)(char* utf8String, int32_t byteLength);
using KeyPressCallback = void(*)(int32_t key);

static bool shouldExit = false;

CS_VISIBLE void ExitGame()
{
	shouldExit = true;
}

CS_VISIBLE void RunGame(InitCallback initCallback, CloseCallback closeCallback,
                        FrameCallback frameCallback, ResizeCallback resizeCallback,
                        TextInputCallback textInputCallback, KeyPressCallback keyPressCallback)
{
	if (SDL_Init(SDL_INIT_VIDEO))
		return;
	
	if (IMG_Init(IMG_INIT_PNG) != IMG_INIT_PNG)
		return;
	
	int contextFlags = SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;
#ifndef NDEBUG
	contextFlags |= SDL_GL_CONTEXT_DEBUG_FLAG;
#endif
	
	SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 4);
	SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 4);
	SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);
	SDL_GL_SetAttribute(SDL_GL_CONTEXT_FLAGS, contextFlags);
	SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, SDL_TRUE);
	SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
	
	SDL_Window* window = SDL_CreateWindow("Poker", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, WIDTH, HEIGHT,
	                                      SDL_WINDOW_RESIZABLE | SDL_WINDOW_OPENGL);
	if (window == nullptr)
		return;
	
	SDL_Surface* icon = IMG_Load("./Res/UI/Icon.png");
	SDL_SetWindowIcon(window, icon);
	
	SDL_GLContext glContext = SDL_GL_CreateContext(window);
	if (glContext == nullptr)
		return;
	
	glewExperimental = GL_TRUE; 
	if (glewInit() != GLEW_OK)
		return;
	
#ifndef NDEBUG
	if (GLEW_ARB_debug_output)
	{
		glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);
		glDebugMessageCallback(OpenGLMessageCallback, nullptr);
	}
#endif
	
	glGetIntegerv(GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT, &UniformBufferOffsetAlignment);
	glGetIntegerv(GL_SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT, &SSBOOffsetAlignment);
	
	glEnable(GL_TEXTURE_CUBE_MAP_SEAMLESS);
	glDisable(GL_CULL_FACE);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	
	stbi_set_unpremultiply_on_load(true);
	
	initCallback();
	resizeCallback(WIDTH, HEIGHT);
	DisplayWidth = WIDTH;
	DisplayHeight = HEIGHT;
	
	using Clock = std::chrono::high_resolution_clock;
	Clock::duration lastFrameTime(0);
	
	GLsync fences[MAX_QUEUED_FRAMES] = { };
	
	shouldExit = false;
	while (!shouldExit)
	{
		Clock::time_point frameStartTime = Clock::now();
		float dt = lastFrameTime.count() * 1E-9f;
		
		SDL_Event event;
		while (SDL_PollEvent(&event))
		{
			switch (event.type)
			{
			case SDL_QUIT:
				shouldExit = true;
				break;
			case SDL_TEXTINPUT:
				textInputCallback(event.text.text, std::strlen(event.text.text));
				break;
			case SDL_KEYDOWN:
				keyPressCallback(event.key.keysym.scancode);
				break;
			case SDL_MOUSEWHEEL:
				MouseWheelX += event.wheel.x;
				MouseWheelY += event.wheel.y;
				break;
			case SDL_WINDOWEVENT:
				switch (event.window.event)
				{
				case SDL_WINDOWEVENT_RESIZED:
					resizeCallback(event.window.data1, event.window.data2);
					glViewport(0, 0, event.window.data1, event.window.data2);
					DisplayWidth = event.window.data1;
					DisplayHeight = event.window.data2;
					break;
				}
				break;
			}
		}
		
		if (fences[FrameQueueIndex])
		{
			glClientWaitSync(fences[FrameQueueIndex], GL_SYNC_FLUSH_COMMANDS_BIT, UINT64_MAX);
			glDeleteSync(fences[FrameQueueIndex]);
		}
		
		frameCallback(dt);
		
		SDL_GL_SwapWindow(window);
		
		fences[FrameQueueIndex] = glFenceSync(GL_SYNC_GPU_COMMANDS_COMPLETE, 0);
		
		FrameIndex++;
		FrameQueueIndex = FrameIndex % MAX_QUEUED_FRAMES;
		
		lastFrameTime = Clock::now() - frameStartTime;
	}
	
	closeCallback();
	
	SDL_GL_DeleteContext(glContext);
	SDL_DestroyWindow(window);
	SDL_FreeSurface(icon);
	SDL_Quit();
}
