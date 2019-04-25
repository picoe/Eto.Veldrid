
  Eto.Veldrid
  ===========

  A simple test project designed to demonstrate embedding the [Veldrid](https://veldrid.dev) graphics library in the [Eto.Forms](https://github.com/picoe/Eto) GUI framework. Based on the Veldrid [Getting Started](https://veldrid.dev/articles/getting-started/intro.html) tutorial, and integrated into Eto in the vein of [etoViewport](https://github.com/philstopford/etoViewport) (thanks Phil!).

  ### Notes
  - Requires OpenTK 3 **no newer than 3.0.1!** NuGet references are already in place, but please don't change the version. OpenGL support must unfortunately rely upon private members of certain OpenTK classes, and although OpenTK 4 isn't out yet, it's all but guaranteed those members won't exist anymore, or will be renamed, or moved, or who knows what. The same is likely true for older versions, for that matter. Doing things the way I've done them is not wise, if it can be avoided, but if there's any other reasonable way to get an OpenGL context set up for Veldrid to make use of, I don't know what it is. Suggestions welcome if anyone knows another way to handle this. See VeldridSurface.VeldridGL for more details.

  - The etoViewport dependency is stored in the repo as a set of binaries for the sake of simplicity.

  - Shaders are written in Vulkan-style GLSL, and cross-compiled to the target backend using [Veldrid.SPIRV](https://github.com/mellinoe/veldrid-spirv).
  
  - Of particular importance is that the Veldrid.SPIRV NuGet package will copy its managed component to the output directory properly, but not the native library it also ships with. This seems to be [a limitation of NuGet](https://stackoverflow.com/a/40652794), unfortunately, so a workaround in the form of a custom build target is necessary. See Directory.Build.props and Directory.Build.targets for the workaround.

  - On a similar note, in those same files you'll find extra properties and targets to clean up the output directories. This is only cosmetic, but Veldrid does pull in its fair share of dependencies, as do .NET Framework versions prior to 4.7.2, and this helps organize things. Feel free to ignore it in your own projects if you disagree.
    - To help the GUI executables find the relocated DLLs, each GUI project has an App.config file with a `<probing>` element containing a `privatePath` attribute that allows assemblies to be found in subdirectories. There are no App.config files in .NET Core projects, however, so if you venture off in that direction you'll need to figure that out yourself.

  - I've tested Vulkan, Direct3D, and OpenGL in WinForms and WPF, and OpenGL and Metal in the Mac project, but only OpenGL in GTK. My Linux testing environment is just a VM with no Vulkan support, so that backend hasn't been tested in that platform. Field reports of success or failure are most welcome. :)

  Browse the code, fiddle with it to your heart's content, and take advantage of whatever's useful, but please don't take this to be the most efficient, production-quality approach to writing custom Eto controls, using Veldrid, or generally doing modern graphics coding.
