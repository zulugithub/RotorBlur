
# RotorBlur / Unity 2023.2.16f1 / Built-in Render Pipeline

![](https://github.com/zulugithub/RotorBlur/blob/main/RotorBlur.png?raw=true "Title")
![](https://github.com/zulugithub/RotorBlur/blob/main/sketch.png?raw=true "Title")

## Introduction

Rotor blur similar to DCS' helicopter rotor blur.

A second camera renders the complex 3D rotor geometry into a “render texture” that is projected onto a simple shell body ("cylinder") and blurred by a custom shader. 

The blur is created wit hollwoing steps:
1. Second camera focuses on rotor and renders only this geometry (layer "rotor") to a low res render-texture (256x256, R32G32B32A32_SFLOAT)
2. Main camera renders shell body (layer "cylinder"). Vertex shader passes rotor's local mesh to fragemnt shader. 
3. Fragment shader rotates P1 by a given amount --> P2
4. Fragment shader calculates P2's render-texture screen space position, then sampling the render-texture at this position
5. The color is weighted by sigma and mixed to the main cameras fragment output  

Shader: 
https://github.com/zulugithub/RotorBlur/blob/main/RotorBlur/Assets/RotationBlurShader.shader

C# script:
https://github.com/zulugithub/RotorBlur/blob/main/RotorBlur/Assets/SetProjectorToShader.cs

## Credits

https://forum.unity.com/threads/what-kind-of-shader-creates-this-blured-helicopter-rotor.1598373/

## License

This software is licensed under the GPLv3 license. A copy of the license can
be found in `LICENSE.txt`


