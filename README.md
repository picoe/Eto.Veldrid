
  Eto.Veldrid
  ===========

  A simple test project designed to demonstrate embedding the [Veldrid](https://veldrid.dev) graphics library in the [Eto](https://github.com/picoe/Eto) GUI framework. Based on the Veldrid [Getting Started](https://veldrid.dev/articles/getting-started/intro.html) tutorial, and integrated into Eto in the vein of [etoViewport](https://github.com/philstopford/etoViewport) (thanks Phil!).

  ### Notes
  - Requires OpenTK 3, **no newer than 3.0.1!** OpenGL support must unfortunately rely upon private members of certain OpenTK classes, and although OpenTK 4 isn't out yet, it's all but guaranteed those members won't exist anymore, or will be renamed, or moved, or who knows what. Doing things the way I've done them is not wise, if it can be avoided, but if there's any other reasonable way to get an OpenGL context set up for Veldrid to make use of, I don't know what it is. Suggestions welcome if anyone knows another way to handle this. See VeldridSurface.VeldridGL for more details.

  - The etoViewport dependency is stored in the repo as a set of binaries for the sake of simplicity.

  - Shaders are written in Vulkan-style GLSL, and cross-compiled to the target backend using [Veldrid.SPIRV](https://github.com/mellinoe/veldrid-spirv).
  
  - Of particular importance is that the Veldrid.SPIRV NuGet package will not copy the native library it ships with to the appropriate output directory. This seems to be a limitation of NuGet, unfortunately, so a workaround in the form of a custom build target is necessary. See Directory.Build.props and Directory.Build.targets for the workaround.

  - On a similar note, in those same files you'll find extra properties and targets to clean up the output directories. This is only cosmetic, but Veldrid does pull in its fair share of dependencies, as do .NET Framework versions prior to 4.7.2, and this helps organize things. Feel free to ignore it in your own projects if you disagree.
    - To help the GUI executables find the relocated DLLs, each GUI project has an App.config file with a `<probing>` element containing a `privatePath` attribute that allows assemblies to be found in subdirectories. There are no App.config files in .NET Core projects, however, so if you venture off in that direction you'll need to figure that out yourself.

  - I've tested Vulkan, Direct3D, and OpenGL in WinForms and WPF, and I've tested OpenGL in the Gtk and Mac projects, but that's it. My testing environment includes a Windows 10 installation on an actual PC, but only a Linux VM with no Vulkan support, and a Mac mini that's one generation too old to support Metal (and, by extension, Mojave), so those backends haven't been tested in those platforms. Field reports of success or failure are most welcome. :)

  Feel free to browse the code, and use it if you feel it's instructive, but please don't take this to be the most efficient, production-quality approach to writing custom Eto controls, using Veldrid, or generally doing modern graphics coding.
