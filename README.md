# glTF Import
###### glTF 2.0 Import plugin for Rhino

## Description

This is a [GL Transmission Format (glTF)](https://github.com/KhronosGroup/glTF) import plugin for Rhino 6 that can import the following flavors of glTF 2.0:
- .glTF with external binary buffers (.bin)
- .glTF with embedded buffers
- .glb

The project depends on the [glTF2Loader](https://www.nuget.org/packages/glTF2Loader/1.1.1-alpha) package [(src)](https://github.com/KhronosGroup/glTF-CSharp-Loader/).

## Current Limitations

This plugin has several limitations, some which will be fixed with time, others which depend on developing the feature support within Rhino.
- No concept of Scene or Node dependency
- No concept of animation
- No extension support
- Vertex Colors not working properly
- No transformations are being applied
- Textures are not mapped correctly
- Rhino currently has no support for PBR Textures, so the main Material type used in glTF needs to be translated to whatever Rhino can visualize. This means Metalness, Roughness, Normal Maps, etc are not compatible with the Rhino Material system
- Rhino meshes do not have the concept of TANGENT Vertex Vectors
- Rhino does not support mesh skinning or morph targets

## Development Status

This plugin is in active development.
Currently, this plugin can import the majority of models provided in the [KhronosGroup Sample Models Repository](https://github.com/KhronosGroup/glTF-Sample-Models). There are three or four models that do not import correctly. Hopefully this can be fixed with time.

The plugin has been tested on Rhino for Windows 6 sr8 and is developed in Visual Studio 2018.

## Contributing

You can contribute to this project in several ways:
- [Submit an issue or feature request](https://github.com/mcneel/glTF/issues)
- Test the plugin
  - Clone this repository
  - Open the solution in Visual Studio 2018
  - Ensure NuGet packages have been restored
  - Start debugging. If it is the first time you are running this, you will need to drag and drop the generated `.rhp` file in the `/bin` directory into Rhino.
  - Run `_Import` and select a `.gltf` or `.glb` file.
- Submit a pull request.
  - Fork this repository
  - Fix issues or add new features
  - Commit and push this to your own remote
  - Submit a pull request

