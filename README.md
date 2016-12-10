# Voronoi Fracture
This Unity Project, focuses on Rigid body destruction of a body when it collides with a projectile. The main scene comprises of a simple table with four legs.
The legs and the table-top are made up of primitive cube objects in Unity.

There is a ball suspended above the table. On running the scene, when the ball hits the table top it create a voronoi diagram that is used
to shatter it into several fragments. The original body is replaced with individual cell meshes that are simulated in physical world.

![Table top shattered by ball](https://nikhilnxvverma1.github.io/voronoi-fracture/resources/images/preview.gif)

The voronoi diagram is computed using Fortune's algorithm which is implemented as a separate console application in C# (under resource-development) before getting integrated in the main project.

For more information about this project, please visit the [Project Website](https://nikhilnxvverma1.github.io/voronoi-fracture/)
