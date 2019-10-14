
  Eto.Veldrid
  ===========

  A simple test project designed to demonstrate embedding the [Veldrid](https://veldrid.dev) graphics library in the [Eto.Forms](https://github.com/picoe/Eto) GUI framework. Based on the Veldrid [Getting Started](https://veldrid.dev/articles/getting-started/intro.html) tutorial, and integrated into Eto in the vein of [etoViewport](https://github.com/philstopford/etoViewport) (thanks Phil!).

  ### Notes
  - Requires OpenTK **no newer than 3.x**! NuGet references are already in place, but please don't change the version. OpenGL support must unfortunately rely upon private members of certain OpenTK classes, and although OpenTK 4 isn't out yet, it's all but guaranteed those members won't exist anymore, or will be renamed, or moved, or who knows what. The same is likely true for older versions, for that matter. Doing things the way I've done them is not wise, if it can be avoided, but if there's any other reasonable way to get an OpenGL context set up for Veldrid to make use of, I don't know what it is. Suggestions welcome if anyone knows another way to handle this. See VeldridSurface.VeldridGL for more details.

  - Shaders are written in Vulkan-style GLSL, and cross-compiled to the target backend using [Veldrid.SPIRV](https://github.com/mellinoe/veldrid-spirv).

  - In Directories.Build.props and .targets you'll find extra properties and targets to clean up the output directories. This is only cosmetic, but Veldrid does pull in its fair share of dependencies, as do .NET Framework versions prior to 4.7.2, and this helps organize things. Feel free to ignore it in your own projects if you disagree.
    - To help the GUI executables find the relocated DLLs, each GUI project has an App.config file with a `<probing>` element containing a `privatePath` attribute that allows assemblies to be found in subdirectories. There are no App.config files in .NET Core projects, however, so if you venture off in that direction you'll need to figure that out yourself.

  - All backends have been tested on all platforms that support them, with two exceptions: Vulkan in macOS (it may be technically possible through MoltenVK, but it hasn't been tested just yet) and OpenGLES on any platform. Please feel free to open issues about your experience with any backend or platform, whether that experience is a success or a failure.

  Browse the code, fiddle with it to your heart's content, and take advantage of whatever's useful, but please don't take this to be the most efficient, production-quality approach to writing custom Eto controls, using Veldrid, or generally doing modern graphics coding.
