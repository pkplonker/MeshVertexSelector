##  Mesh Vertex Selector

#### OverviewOverview
The Mesh Vertex Selector is a Unity Editor script designed to visually aid developers and designers by providing labels on meshes in the scene view. Specifically, this script focuses on labeling vertices hit by a ray. It not only showcases the exact position of these hits but also provides measurements in local or global space, and provides persistent settings for user ease of use.

#### FeaturesFeatures

-  Customizable Hit Labels: Labels drawn at hit points on the mesh display the position (X, Y, Z) of the hit point. Each coordinate is colored distinctively: X in red, Y in green, and Z in blue.

-  Measurement Space Selection: Developers can choose to see the measurements in local or global space.

-  Persistent User Settings: The script can remember the user's settings, including their preference for showing hit positions and the desired measurement space. These settings are saved and automatically loaded between sessions.

- Multi-Color Labeling: A utility function exists to draw labels in multiple colors, offering clarity and differentiation for data points.

- Undo/Redo Integration: Integrates with Unity's Undo system. Actions like selecting a vertex can be undone or redone seamlessly.

#### Usage/Setup
- Setup • Ensure the script is located in an Editor folder within your Unity project. It can be accesed by Tools/Vertex Selector

- Drawing Hit Labels • Select a mesh in the scene view. • Use the DrawHitLabel(Vector3 hitpoint) function to visualize the hit point on the mesh.

- Customizing Label Appearance • Customize the background color, text color, font size, and alignment of the label using the DrawHitLabel function.

- Measurement Space • To toggle between local and global space measurements, adjust the settings.ShowMeasurementInLocal boolean. True signifies local space, and false signifies global space.

- Saving and Loading User Preferences • User preferences can be saved using the SaveSettingsToPrefs() function and loaded with LoadSettingsFromPrefs().

- Detecting Vertex Hits • The CheckForVertexHit(Ray ray) function checks for the closest vertex hit by a given ray on the selected object's mesh.

#### Recommendations
For improved performance and more precise vertex detection, it's advisable to use this tool on optimized meshes with a limited number of vertices. Regularly save the scene to ensure all data is backed up when using this tool.

#### Conclusion
The Mesh Vertex Selector is a tool for developers and designers seeking greater visibility and understanding of their 3D meshes in Unity. Its customization options, persistent settings, and vertex detection make it a valuable asset in the Unity editor environment.
