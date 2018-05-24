#!/bin/bash
mkdir -p .build

vs=(Card.vs.glsl Board.vs.glsl PlayerName.vs.glsl Sky.vs.glsl Chip.vs.glsl ChipShadow.vs.glsl BoardShadow.vs.glsl CardShadow.vs.glsl Blur.vs.glsl)
fs=(Card.fs.glsl Board.fs.glsl PlayerName.fs.glsl Sky.fs.glsl Chip.fs.glsl CardShadow.fs.glsl Blur.fs.glsl)

preamble=$'#version 440 core\n#extension GL_GOOGLE_include_directive:enable\n#line 1\n'

for shader in ${vs[@]}; do
	echo -e "$preamble" "$(cat ${shader})" | glslangValidator --stdin -E -S vert > .build/${shader}
	glslangValidator -S vert .build/${shader}
done

for shader in ${fs[@]}; do
	echo -e "$preamble" "$(cat ${shader})" | glslangValidator --stdin -E -S frag > .build/${shader}
	glslangValidator -S frag .build/${shader}
done

7z a -tzip Shaders. ./.build/* > /dev/null
