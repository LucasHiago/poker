#pragma once

#ifdef _WIN32
#define CS_VISIBLE extern "C" __declspec(dllexport)
#else
#define CS_VISIBLE extern "C"
#endif
