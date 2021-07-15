# Multigrid feature based terrain generationwith erosion

This is the implementation of the thesis "Multigrid feature based terrain generationwith erosion" by Thomas Lund Mathisen. 

## Structure

* Editor: Contains scripts for Terrain window and visualization of the splines
* ErosionSettings: All erosion settings as ScriptableObjects
* Images: Images of the terrain and textures during generation if Save Images is set on the terrain object
* MeshTerrainEditor: Used to export terrain to .obj file
* Prefabs: Some splines one can use
* Scenes: The scenes we used in our results
* Scripts: Our C# scripts
* Shaders: Our compute shader
* Shaders/Materials: Our materials and splat shader

### Scripts

The important scripts

* Erosion/HydraulicErosion: Our Erosion simulation class

* Terrain/Laplace: Our multigrid solver
* Terrain/SplineTerrain: Our main file using the multigrid and hydraulicErosion
* Terrain/TerrainVisualization: Copy our terrain to the unity terrain after generation is complete

* Terrain/Rasterizing/TesselateSplineLineAndGradients: Convert splines to vertecies and indices.
* Terrain/Rasterizing/RasterizeWithShader: Using the file above and a compute shader converting splines to textures.

* Terrain/Splines/BezierSpline: Our feature splines
* Terrain/Splines/SplineMetaPoint: Our meta points

* Bezier: Our Bézier definition
* CameraScreenshot: Used on cameras for capturing images and saving in /Images
* Terrain 

## How to operate

You need an empty with the following scripts: 

* Terrain Visualizer
* Spline Terrain
* Hydraulic Erosion - if you use erosion
* Laplace
* Rasterize With Shader

Then you need a terrain object as a child of this object referd to in Terrain Visualizer. You need an empty object with the tag TerrainFeatures where all the splines are stored (generated when using the terrain editor window). Only splines in this object is used in the generation process. 

Open the terrain editor window from Window/Terrain Editor. Add a spline with the button and then you can render the terrain. After saving you can get the texture back by pressing Load texture. Render images takes a screenshot with each camera with the Camera Screenshot script and renames the files depending on the terrain settings and camera name. Can be used to render the terrain from many angles at the same time. 

Some sliders are provided in the terrain editor window when selecting a spline or a meta point. All the data worth changing is available in the inspector as well when selecting a spline. 