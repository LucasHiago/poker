#include "API.h"

#include <cstdint>
#include <SDL2/SDL.h>

int32_t MouseWheelX = 0;
int32_t MouseWheelY = 0;

struct MouseState
{
	int32_t CursorX;
	int32_t CursorY;
	int32_t ScrollX;
	int32_t ScrollY;
	uint8_t LeftButtonPressed;
	uint8_t RightButtonPressed;
};

CS_VISIBLE MouseState GetMouseState()
{
	MouseState mouseState;
	
	uint32_t buttonState = SDL_GetMouseState(&mouseState.CursorX, &mouseState.CursorY);
	mouseState.LeftButtonPressed = (buttonState & SDL_BUTTON_LMASK) ? 1 : 0;
	mouseState.RightButtonPressed = (buttonState & SDL_BUTTON_RMASK) ? 1 : 0;
	mouseState.ScrollX = MouseWheelX;
	mouseState.ScrollY = MouseWheelY;
	
	return mouseState;
}

#pragma pack(push, 1)
struct KeyboardState
{
	uint32_t NumKeys;
	const uint8_t* KeyStates;
};
#pragma pack(pop)

CS_VISIBLE void GetKeyboardState(KeyboardState* keyboardState)
{
	int numKeys;
	keyboardState->KeyStates = SDL_GetKeyboardState(&numKeys);
	keyboardState->NumKeys = numKeys;
}
