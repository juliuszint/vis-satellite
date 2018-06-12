## Project for Visualisierung und HCI class
This application visualizes satellites orbiting earth. The only library used is the OpenTK library for low level access to OpenGL from C#. OpenTK is able to run on Windows with the .Net Framework and under Linux or OSX with Mono. The codebase therefor should compile and run without any modifications under all of these operating systems (tested only on OS X and Windows). 

### Using the program

![alt text](images/vissatellite.png)

On Windows just double click the `vissatellite.exe` and on OS X start it by typing `mono vissatellite.exe` into the Terminal app. Vissatellite makes use of the Console window as well as the GUI. Moving around in space is done in the GUI window. Once focused use `W` and `S` to move forward or backward, `Arrow Up` and `Arrow Down` to control the Pitch and `Arrow Left` and `Arrow Right` to manipulate the Yaw Axis. Roll can also be changed by using `A` and `D` just like in any flight simulator.

If there is a satellite on your Screen that you want to know more about just select it with a `Left Mouseclick`. Once selected, the color of the satellite changes to red and additional information appears on the Console.

The console window does not only show additional information, it can also be used to manipulate the simulation.

TODO:

### Code
#### Embedded resources
All additional resources that the program needs for execution are part of the assemblies. For each type of resource there is a toplevel directory.

    /meshes
    /shader
    /simdata
    /texture

The `meshes` directory contains Wavefront (.obj) files for all the geometry used. The `shader` directory contains source code for a fragment and vertex shader with very primitive lighting. The `simdata` directory contains a single `.txt` file with all the satellite informations and the `textures` directory contains colortextures as well as a empty normal texture.

#### Source
Five `.cs` files make up the entire source code for this small program. 

`Utils.cs` is a small file with two extension methods for working with embedded resources. One for loading the entire resource as string and the other for opening a stream to a embedded resource. One for loading the entire resource as string and the other for opening a stream to a embedded resource.

`Program.cs` contains only 2 important lines of code that create a instance of the `SatelliteUniverse` class and execute the `Run` method.

`Wavefront.cs` has a single static class named `Wavefront` with only a single used public method `ObjectVertexData Load(string filename)`. It parses the specified wavefront file, Calculates the Tangents and Bitangents using the uv coordinates and also evaluates the absolute maximum value for all vertices. This is used for creating a bounding sphere for ray picking.
