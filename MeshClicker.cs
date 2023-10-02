using UnityEngine;
using UnityEditor;

public class MeshClicker : EditorWindow
{
	public class VertexSelectorSettings : ScriptableObject
	{
		public bool ShowHitPosition = false;
		public bool ShowMeasurementInLocal = false;
		public Vector3 HitVertex = Vector3.positiveInfinity;
	}

	private GameObject selectedObject;
	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;
	private VertexSelectorSettings settings;
	private SerializedObject serializedSettings;
	private SerializedProperty propShowHitPosition;
	private SerializedProperty propShowMeasurementInLocal;
	private SerializedProperty prophitVertex;

	private const string PrefKeyShowHitPosition = "VertexSelectorWindow.ShowHitPosition";

	private const string PrefKeyShowMeasurementInLocal = "VertexSelectorWindow.ShowMeasurementInLocal";

	[MenuItem("Tools/Vertex Selector")]
	public static void ShowWindow() => GetWindow<MeshClicker>("Vertex Selector");

	private void OnGUI()
	{
		GUILayout.Label("Select a GameObject with a Mesh", EditorStyles.boldLabel);

		selectedObject =
			(GameObject) EditorGUILayout.ObjectField("Target Object", selectedObject, typeof(GameObject), true);

		if (selectedObject != null)
		{
			if (selectedObject.TryGetComponent<MeshFilter>(out var meshFilter))
			{
				mesh = meshFilter.sharedMesh;
				vertices = mesh.vertices;
				triangles = mesh.triangles;
			}
			else
			{
				mesh = null;
			}
		}

		serializedSettings?.Update();

		EditorGUILayout.PropertyField(propShowHitPosition, new GUIContent("Show Hit Position"));
		if (propShowHitPosition.boolValue)
		{
			EditorGUILayout.PropertyField(propShowMeasurementInLocal, new GUIContent("In Local Space"));
		}

		serializedSettings?.ApplyModifiedProperties();

		Repaint();
		Debug.Log(settings.HitVertex);
	}

	private void OnSceneGUI(SceneView sceneView)
	{
		if (mesh == null) return;

		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			CheckForVertexHit(ray);
			SceneView.RepaintAll();
		}

		if (settings.HitVertex != Vector3.positiveInfinity)
		{
			Handles.color = Color.red;
			Handles.SphereHandleCap(0, settings.HitVertex, Quaternion.identity, 0.1f, EventType.Repaint);
		}

		if (settings.HitVertex != Vector3.positiveInfinity && settings.ShowHitPosition)
		{
			DrawHitLabel(settings.HitVertex);
		}
	}

	private void DrawHitLabel(Vector3 hitpoint)
	{
		var currentTransform = selectedObject.transform;

		if (currentTransform == null) return;

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

		Vector3 adjustedHitPoint = hitpoint;

		if (settings.ShowMeasurementInLocal && currentTransform != null)
		{
			adjustedHitPoint = currentTransform.InverseTransformPoint(hitpoint);
		}

		var xDelta = adjustedHitPoint.x;
		var yDelta = adjustedHitPoint.y;
		var zDelta = adjustedHitPoint.z;
		var precision = 2;

		string mainText = "";
		string[] subTexts =
		{
			string.Format("X:{0:F" + precision + "}", xDelta),
			string.Format("Y:{0:F" + precision + "}", yDelta),
			string.Format("Z:{0:F" + precision + "}", zDelta)
		};

		Color[] subColors =
		{
			Color.red,
			Color.green,
			Color.blue
		};

		DrawMultiColorLabel(hitpoint, mainText, textColor, subTexts, subColors,
			labelStyle);
	}

	public static void DrawMultiColorLabel(Vector3 position, string mainText, Color mainColor, string[] subTexts,
		Color[] subColors, GUIStyle style)
	{
		float yOffset = style.lineHeight * 1.01f;
		Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

		GUIStyle noBackgroundStyle = new GUIStyle(style);

		if (!string.IsNullOrEmpty(mainText))
		{
			noBackgroundStyle.normal.textColor = mainColor;
			Vector3 worldPositionForMain = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
			Handles.Label(worldPositionForMain, mainText, noBackgroundStyle);
			guiPosition.y += yOffset; // Adjust yOffset only if mainText is drawn
		}

		for (int i = 0; i < subTexts.Length; i++)
		{
			noBackgroundStyle.normal.textColor = subColors[i];
			Vector3 worldPositionForSub = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
			Handles.Label(worldPositionForSub, subTexts[i], noBackgroundStyle);
			guiPosition.y += yOffset;
		}
	}

	private void OnEnable()
	{
		if (settings == null)
		{
			settings = CreateInstance<VertexSelectorSettings>();
		}

		LoadSettingsFromPrefs();

		serializedSettings = new SerializedObject(settings);
		SceneView.duringSceneGui -= OnSceneGUI;
		SceneView.duringSceneGui += OnSceneGUI;
		settings.ShowHitPosition = EditorPrefs.GetBool(PrefKeyShowHitPosition, false);
		settings.ShowMeasurementInLocal = EditorPrefs.GetBool(PrefKeyShowMeasurementInLocal, false);
		propShowHitPosition = serializedSettings.FindProperty("ShowHitPosition");
		propShowMeasurementInLocal = serializedSettings.FindProperty("ShowMeasurementInLocal");
		prophitVertex = serializedSettings.FindProperty("HitVertex");
		Undo.undoRedoPerformed -= OnUndoRedo;
		Undo.undoRedoPerformed += OnUndoRedo;
	}

	private void OnDisable()
	{
		SaveSettingsToPrefs();
		SceneView.duringSceneGui -= OnSceneGUI;
		EditorPrefs.SetBool(PrefKeyShowHitPosition, settings.ShowHitPosition);
		EditorPrefs.SetBool(PrefKeyShowMeasurementInLocal, settings.ShowMeasurementInLocal);
		if (settings)
		{
			DestroyImmediate(settings);
		}

		Undo.undoRedoPerformed -= OnUndoRedo;
	}

	private void LoadSettingsFromPrefs()
	{
		settings.ShowHitPosition = EditorPrefs.GetBool(PrefKeyShowHitPosition, false);
		settings.ShowMeasurementInLocal = EditorPrefs.GetBool(PrefKeyShowMeasurementInLocal, false);
	}

	private void SaveSettingsToPrefs()
	{
		EditorPrefs.SetBool(PrefKeyShowHitPosition, settings.ShowHitPosition);
		EditorPrefs.SetBool(PrefKeyShowMeasurementInLocal, settings.ShowMeasurementInLocal);
	}

	private void CheckForVertexHit(Ray ray)
	{
		float closestIntersection = float.MaxValue;
		Vector3 newVertexPosition = Vector3.zero;
		bool vertexFound = false;

		for (var i = 0; i < triangles.Length; i += 3)
		{
			Vector3 v0 = selectedObject.transform.TransformPoint(vertices[triangles[i]]);
			Vector3 v1 = selectedObject.transform.TransformPoint(vertices[triangles[i + 1]]);
			Vector3 v2 = selectedObject.transform.TransformPoint(vertices[triangles[i + 2]]);

			if (!RayIntersectsTriangle(ray, v0, v1, v2, out var intersection)) continue;

			float intersectionDistance = Vector3.Distance(ray.origin, intersection);
			if (intersectionDistance < closestIntersection)
			{
				closestIntersection = intersectionDistance;

				// Determine the closest vertex of the intersected triangle based on the intersection point
				Vector3 localIntersection = selectedObject.transform.InverseTransformPoint(intersection);
				float d0 = Vector3.Distance(localIntersection, vertices[triangles[i]]);
				float d1 = Vector3.Distance(localIntersection, vertices[triangles[i + 1]]);
				float d2 = Vector3.Distance(localIntersection, vertices[triangles[i + 2]]);

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

				newVertexPosition = selectedObject.transform.TransformPoint(newVertexPosition);
				vertexFound = true;
			}
		}

		if (vertexFound && prophitVertex.vector3Value != newVertexPosition)
		{
			Debug.Log("Saving new vert pos ");
			Undo.RecordObject(settings, "Select Vertex");
			serializedSettings.Update();

			prophitVertex.vector3Value = newVertexPosition;

			serializedSettings.ApplyModifiedProperties();
		}
	}

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