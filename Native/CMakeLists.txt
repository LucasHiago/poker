cmake_minimum_required(VERSION 3.5)
project(PokerNative)

set(CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}")

if (WIN32)
	set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/../Poker/bin/${CMAKE_BUILD_TYPE})
	add_definitions(/Gz)
else()
	set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/../Poker/bin/${CMAKE_BUILD_TYPE}/netcoreapp2.0)
endif()

find_package(SDL2 REQUIRED)
find_package(GLEW REQUIRED)
find_package(OpenGL REQUIRED)

add_library(Native SHARED Src/API.h Src/Window.cpp Src/SpriteBatch.cpp Src/Texture2D.cpp Src/Utils.h Src/Utils.cpp
	Src/Input.cpp Src/Mesh.cpp Src/Shader.cpp Src/UniformBuffer.cpp Src/Graphics.cpp Src/Skybox.cpp Src/ChipsBuffer.cpp
	Src/ShadowMap.cpp Src/ShadowMatrixBuffer.cpp Src/BlurFB.cpp)

target_include_directories(Native SYSTEM PUBLIC ${SDL2_INCLUDE_DIRS} ${GLEW_INCLUDE_DIRS} ${OPENGL_INCLUDE_DIRS} ${CMAKE_SOURCE_DIR}/Inc)
target_link_libraries(Native ${SDL2_LIBRARY} ${GLEW_LIBRARY} ${OPENGL_LIBRARY})
