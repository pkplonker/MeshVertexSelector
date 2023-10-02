using UnityEngine;
using UnityEditor;

public class MeshClicker : EditorWindow
{
	// Define a class to hold settings related to vertex selection.
	public class VertexSelectorSettings : ScriptableObject
	{
		public bool ShowHitPosition;
		public bool ShowMeasurementInLocal;

		public Vector3
			HitVertex = Vector3
				.positiveInfinity; // The hit vertex position. Initialized to positive infinity to indicate 'not set'.
	}

	// Variables to store the selected game object and its mesh data.
	private GameObject selectedObject;
	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;

	private VertexSelectorSettings settings; // The settings instance.

	// SerializedObject and SerializedProperty are used to edit settings in a way that plays nice with Unity's editor, including undo functionality.
	private SerializedObject serializedSettings;
	private SerializedProperty propShowHitPosition;
	private SerializedProperty propShowMeasurementInLocal;
	private SerializedProperty prophitVertex;

	// Constants for EditorPrefs keys to remember user settings.
	private const string PREF_KEY_SHOW_HIT_POSITION = "VertexSelectorWindow.ShowHitPosition";
	private const string PREF_KEY_SHOW_MEASUREMENT_IN_LOCAL = "VertexSelectorWindow.ShowMeasurementInLocal";

	// Method to show the window.
	[MenuItem("Tools/Vertex Selector")]
	public static void ShowWindow() => GetWindow<MeshClicker>("Vertex Selector");

	// UI definition for the window.
	private void OnGUI()
	{
		// Provide instructions and a way to select a GameObject.
		GUILayout.Label("Select a GameObject with a Mesh", EditorStyles.boldLabel);
		selectedObject =
			(GameObject) EditorGUILayout.ObjectField("Target Object", selectedObject, typeof(GameObject), true);

		// When a GameObject is selected, attempt to get its mesh data.
		if (selectedObject != null)
		{
			var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
			if (meshFilters.Length > 0)
			{
				// Get the first mesh for display, but you may want to expand this 
				// to handle multiple meshes better in your UI.
				mesh = meshFilters[0].sharedMesh;
				vertices = mesh.vertices;
				triangles = mesh.triangles;
			}
			else
			{
				mesh = null; // If no mesh is found, clear the mesh variable.
			}
		}

		serializedSettings?.Update(); // Ensure the SerializedObject is updated to reflect the latest data.

		// Create toggles for user settings.
		EditorGUILayout.PropertyField(propShowHitPosition, new GUIContent("Show Hit Position"));
		if (propShowHitPosition.boolValue)
		{
			EditorGUILayout.PropertyField(propShowMeasurementInLocal, new GUIContent("In Local Space"));
		}

		serializedSettings
			?.ApplyModifiedProperties(); // Apply changes made to the SerializedObject back to the original object.

		Repaint(); // Ensure the editor window is refreshed.
		Debug.Log(settings.HitVertex); // Debug the hit vertex value.
	}

	// This method will be called during Scene view rendering.
	private void OnSceneGUI(SceneView sceneView)
	{
		// If no mesh, skip processing.
		if (mesh == null) return;

		// Check for mouse click in the scene.
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current
				.mousePosition); // Convert mouse click to a world-space ray.
			CheckForVertexHit(ray); // Check if the ray hits a vertex.
			SceneView.RepaintAll(); // Refresh Scene view to reflect any changes.
		}

		// If a vertex is hit, draw a visual indicator.
		if (settings.HitVertex != Vector3.positiveInfinity)
		{
			Handles.color = Color.red;
			Handles.SphereHandleCap(0, settings.HitVertex, Quaternion.identity, 0.1f, EventType.Repaint);
		}

		// Optionally, display a label showing the hit position.
		if (settings.HitVertex != Vector3.positiveInfinity && settings.ShowHitPosition)
		{
			DrawHitLabel(settings.HitVertex);
		}
	}

	// Draws a label at the provided hit point.
	private void DrawHitLabel(Vector3 hitpoint)
	{
		// Get the transform of the currently selected object.
		var currentTransform = selectedObject.transform;

		// If there's no transform, return early.
		if (currentTransform == null) return;

		// Define the label styles.
		var bgColor = Texture2D.grayTexture;
		var textColor = Color.magenta;
		var labelStyle = new GUIStyle
		{
			normal =
			{
				textColor = textColor,
				background = bgColor,
			},
			fontSize = 30,
			alignment = TextAnchor.UpperCenter,
		};

		// Initialize the hit point position.
		Vector3 adjustedHitPoint = hitpoint;

		// Convert the global hit point to local coordinates if required.
		if (settings.ShowMeasurementInLocal && currentTransform != null)
		{
			adjustedHitPoint = currentTransform.InverseTransformPoint(hitpoint);
		}

		// Extract individual axis values.
		var xDelta = adjustedHitPoint.x;
		var yDelta = adjustedHitPoint.y;
		var zDelta = adjustedHitPoint.z;
		var precision = 2;

		// Prepare the main text and sub-texts to be shown.
		string mainText = "";
		string[] subTexts =
		{
			string.Format("X:{0:F" + precision + "}", xDelta),
			string.Format("Y:{0:F" + precision + "}", yDelta),
			string.Format("Z:{0:F" + precision + "}", zDelta)
		};

		// Define colors corresponding to each axis.
		Color[] subColors =
		{
			Color.red,
			Color.green,
			Color.blue
		};

		// Draw the label with multiple colors.
		DrawMultiColorLabel(hitpoint, mainText, textColor, subTexts, subColors, labelStyle);
	}

	// Draws a label with a main text and sub-texts with distinct colors.
	public static void DrawMultiColorLabel(Vector3 position, string mainText, Color mainColor, string[] subTexts,
		Color[] subColors, GUIStyle style)
	{
		// Offset to space out the text lines.
		float yOffset = style.lineHeight * 1.01f;

		// Convert the world point to a GUI point.
		Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

		GUIStyle noBackgroundStyle = new GUIStyle(style);

		// Draw the main text if provided.
		if (!string.IsNullOrEmpty(mainText))
		{
			noBackgroundStyle.normal.textColor = mainColor;
			Vector3 worldPositionForMain = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
			Handles.Label(worldPositionForMain, mainText, noBackgroundStyle);
			guiPosition.y += yOffset;
		}

		// Draw each of the sub-texts with their respective colors.
		for (int i = 0; i < subTexts.Length; i++)
		{
			noBackgroundStyle.normal.textColor = subColors[i];
			Vector3 worldPositionForSub = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
			Handles.Label(worldPositionForSub, subTexts[i], noBackgroundStyle);
			guiPosition.y += yOffset;
		}
	}

	// Event handler for when the script is enabled.
	private void OnEnable()
	{
		// Create settings instance if it doesn't exist.
		if (settings == null)
		{
			settings = CreateInstance<VertexSelectorSettings>();
		}

		// Load settings from the editor's preferences.
		LoadSettingsFromPrefs();

		// Serialize the settings object.
		serializedSettings = new SerializedObject(settings);

		// Manage the SceneGUI callbacks.
		SceneView.duringSceneGui -= OnSceneGUI;
		SceneView.duringSceneGui += OnSceneGUI;

		// Fetch saved preferences.
		settings.ShowHitPosition = EditorPrefs.GetBool(PREF_KEY_SHOW_HIT_POSITION, false);
		settings.ShowMeasurementInLocal = EditorPrefs.GetBool(PREF_KEY_SHOW_MEASUREMENT_IN_LOCAL, false);

		// Find properties in the serialized settings.
		propShowHitPosition = serializedSettings.FindProperty("ShowHitPosition");
		propShowMeasurementInLocal = serializedSettings.FindProperty("ShowMeasurementInLocal");
		prophitVertex = serializedSettings.FindProperty("HitVertex");

		// Manage Undo/Redo callbacks.
		Undo.undoRedoPerformed -= OnUndoRedo;
		Undo.undoRedoPerformed += OnUndoRedo;
	}

	// Event handler for when the script is disabled.
	private void OnDisable()
	{
		// Save settings to the editor's preferences.
		SaveSettingsToPrefs();

		// Unsubscribe from SceneGUI callbacks.
		SceneView.duringSceneGui -= OnSceneGUI;

		// Save settings in the editor's preferences.
		EditorPrefs.SetBool(PREF_KEY_SHOW_HIT_POSITION, settings.ShowHitPosition);
		EditorPrefs.SetBool(PREF_KEY_SHOW_MEASUREMENT_IN_LOCAL, settings.ShowMeasurementInLocal);

		// Destroy the settings instance.
		if (settings)
		{
			DestroyImmediate(settings);
		}

		// Unsubscribe from Undo/Redo callbacks.
		Undo.undoRedoPerformed -= OnUndoRedo;
	}

	// Load settings from the editor's preferences.
	private void LoadSettingsFromPrefs()
	{
		settings.ShowHitPosition = EditorPrefs.GetBool(PREF_KEY_SHOW_HIT_POSITION, false);
		settings.ShowMeasurementInLocal = EditorPrefs.GetBool(PREF_KEY_SHOW_MEASUREMENT_IN_LOCAL, false);
	}

	// Save settings to the editor's preferences.
	private void SaveSettingsToPrefs()
	{
		EditorPrefs.SetBool(PREF_KEY_SHOW_HIT_POSITION, settings.ShowHitPosition);
		EditorPrefs.SetBool(PREF_KEY_SHOW_MEASUREMENT_IN_LOCAL, settings.ShowMeasurementInLocal);
	}

	private void CheckForVertexHit(Ray ray)
	{
		// Initialize the closest intersection distance to a large value.
		float closestIntersection = float.MaxValue;

		// Create a placeholder for the position of the newly detected vertex.
		Vector3 newVertexPosition = Vector3.zero;

		// A flag to check if a vertex was found in the ray's path.
		bool vertexFound = false;
		var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
		foreach (var meshFilter in meshFilters)
		{
			// Update the mesh and associated vertices and triangles for each MeshFilter
			mesh = meshFilter.sharedMesh;
			vertices = mesh.vertices;
			triangles = mesh.triangles;
			// Iterate over the triangles, checking every 3 vertices (since triangles have 3 points).
			for (var i = 0; i < triangles.Length; i += 3)
			{
				// Convert the local space vertices of the triangle to world space.
				Vector3 v0 = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
				Vector3 v1 = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
				Vector3 v2 = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);

				// Check if the given ray intersects with the current triangle.
				// If not, continue to the next triangle.
				if (!RayIntersectsTriangle(ray, v0, v1, v2, out var intersection)) continue;

				// Calculate the distance from the ray's origin to the intersection point.
				float intersectionDistance = Vector3.Distance(ray.origin, intersection);

				// If the current intersection is closer than any previous intersection.
				if (intersectionDistance < closestIntersection)
				{
					closestIntersection = intersectionDistance;

					// Determine the vertex of the intersected triangle that is closest to the intersection point.
					Vector3 localIntersection = meshFilter.transform.InverseTransformPoint(intersection);
					float d0 = Vector3.Distance(localIntersection, vertices[triangles[i]]);
					float d1 = Vector3.Distance(localIntersection, vertices[triangles[i + 1]]);
					float d2 = Vector3.Distance(localIntersection, vertices[triangles[i + 2]]);

					// Assign the closest vertex to newVertexPosition.
					if (d0 < d1 && d0 < d2)
					{
						newVertexPosition = vertices[triangles[i]];
					}
					else if (d1 < d0 && d1 < d2)
					{
						newVertexPosition = vertices[triangles[i + 1]];
					}
					else
					{
						newVertexPosition = vertices[triangles[i + 2]];
					}
					// Convert the new vertex position to world space.
					newVertexPosition = meshFilter.transform.TransformPoint(newVertexPosition);
					vertexFound = true;
				}
			}
		}

		// If a vertex was found and it's a different position than the last stored one
		if (vertexFound && prophitVertex.vector3Value != newVertexPosition)
		{
			// Log the detection of a new vertex position.
			Debug.Log("Saving new vert pos ");

			// Record the change for undo functionality.
			Undo.RecordObject(settings, "Select Vertex");

			// Update the serialized settings.
			serializedSettings.Update();

			// Assign the new vertex position to the serialized property.
			prophitVertex.vector3Value = newVertexPosition;

			// Apply the changes made to the serialized settings.
			serializedSettings.ApplyModifiedProperties();
		}
	}

	// Event handler for the Undo/Redo action, triggering a repaint of the scene.
	private void OnUndoRedo() => SceneView.RepaintAll();

	private bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hit)
	{
		hit = Vector3.zero;

		// Implementation of Möller–Trumbore intersection algorithm,
		// referenced translation to C# https://discussions.unity.com/t/a-fast-triangle-triangle-intersection-algorithm-for-unity/126010/4
		Vector3 h, s, q;
		float a, f, u, v;
		Vector3 e1 = v1 - v0;
		Vector3 e2 = v2 - v0;
		h = Vector3.Cross(ray.direction, e2);
		a = Vector3.Dot(e1, h);

		const float EPSILON = 0.000001f;
		if (a > -EPSILON && a < EPSILON)
			return false;
		f = 1.0f / a;
		s = ray.origin - v0;
		u = f * Vector3.Dot(s, h);
		if (u < 0.0f || u > 1.0f)
			return false;
		q = Vector3.Cross(s, e1);
		v = f * Vector3.Dot(ray.direction, q);
		if (v < 0.0f || u + v > 1.0f)
			return false;
		float t = f * Vector3.Dot(e2, q);
		if (t > EPSILON)
		{
			hit = ray.origin + ray.direction * t;
			return true;
		}

		return false;
	}
}
