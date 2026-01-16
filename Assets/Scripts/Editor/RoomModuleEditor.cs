using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RoomModule))]
public class RoomModuleEditor : Editor
{
    private RoomModule roomModule;
    private SerializedProperty prefabProp;
    private SerializedProperty dimensionsProp;
    private SerializedProperty socketsProp;
    private SerializedProperty baseWeightProp;
    private SerializedProperty difficultyRangeProp;
    private SerializedProperty tagsProp;

    private bool showSocketsFoldout = true;
    private bool showTagsFoldout = true;
    private bool showValidationFoldout = false;

    private void OnEnable()
    {
        roomModule = (RoomModule)target;
        prefabProp = serializedObject.FindProperty("prefab");
        dimensionsProp = serializedObject.FindProperty("dimensions");
        socketsProp = serializedObject.FindProperty("sockets");
        baseWeightProp = serializedObject.FindProperty("baseWeight");
        difficultyRangeProp = serializedObject.FindProperty("difficultyRange");
        tagsProp = serializedObject.FindProperty("tags");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBasicProperties();
        DrawSocketConfiguration();
        DrawTagsConfiguration();
        DrawValidationSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicProperties()
    {
        EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(prefabProp);
        EditorGUILayout.PropertyField(dimensionsProp);
        EditorGUILayout.PropertyField(baseWeightProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Difficulty Range");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(difficultyRangeProp.GetArrayElementAtIndex(0), GUIContent.none);
        EditorGUILayout.LabelField("to", GUILayout.Width(20));
        EditorGUILayout.PropertyField(difficultyRangeProp.GetArrayElementAtIndex(1), GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void DrawSocketConfiguration()
    {
        showSocketsFoldout = EditorGUILayout.Foldout(showSocketsFoldout, "Socket Configuration", true);

        if (showSocketsFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Sockets ({socketsProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                AddNewSocket();
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < socketsProp.arraySize; i++)
            {
                DrawSocketElement(i);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
    }

    private void DrawSocketElement(int index)
    {
        SerializedProperty socketProp = socketsProp.GetArrayElementAtIndex(index);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Socket {index + 1}", EditorStyles.boldLabel);

        if (GUILayout.Button("↑", GUILayout.Width(20)) && index > 0)
        {
            socketsProp.MoveArrayElement(index, index - 1);
        }

        if (GUILayout.Button("↓", GUILayout.Width(20)) && index < socketsProp.arraySize - 1)
        {
            socketsProp.MoveArrayElement(index, index + 1);
        }

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            socketsProp.DeleteArrayElementAtIndex(index);
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Socket properties
        SerializedProperty directionProp = socketProp.FindPropertyRelative("direction");
        SerializedProperty typeProp = socketProp.FindPropertyRelative("type");
        SerializedProperty idProp = socketProp.FindPropertyRelative("id");
        SerializedProperty weightProp = socketProp.FindPropertyRelative("weight");

        EditorGUILayout.PropertyField(directionProp);
        EditorGUILayout.PropertyField(typeProp);
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(weightProp);

        EditorGUILayout.EndVertical();
    }

    private void DrawTagsConfiguration()
    {
        showTagsFoldout = EditorGUILayout.Foldout(showTagsFoldout, "Tags", true);

        if (showTagsFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Tags ({tagsProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "NewTag";
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(tagsProp.GetArrayElementAtIndex(i), GUIContent.none);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    break; // Exit loop to avoid index issues
                }

                EditorGUILayout.EndHorizontal();
            }

            // Quick tag buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Tags:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Start"))
                AddTagIfNotExists("start");

            if (GUILayout.Button("Boss"))
                AddTagIfNotExists("boss");

            if (GUILayout.Button("Treasure"))
                AddTagIfNotExists("treasure");

            if (GUILayout.Button("Special"))
                AddTagIfNotExists("special");

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
    }

    private void DrawValidationSection()
    {
        showValidationFoldout = EditorGUILayout.Foldout(showValidationFoldout, "Validation", true);

        if (showValidationFoldout)
        {
            EditorGUI.indentLevel++;

            List<string> validationErrors = ValidateRoomModule();

            if (validationErrors.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ All validation checks passed!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {validationErrors.Count} validation errors:", MessageType.Warning);

                foreach (string error in validationErrors)
                {
                    EditorGUILayout.LabelField("• " + error, EditorStyles.miniLabel);
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    private void AddNewSocket()
    {
        socketsProp.InsertArrayElementAtIndex(socketsProp.arraySize);
        SerializedProperty newSocket = socketsProp.GetArrayElementAtIndex(socketsProp.arraySize - 1);

        // Set default values
        newSocket.FindPropertyRelative("direction").enumValueIndex = 0; // North
        newSocket.FindPropertyRelative("type").enumValueIndex = 0; // Entrance
        newSocket.FindPropertyRelative("id").stringValue = "default";
        newSocket.FindPropertyRelative("weight").intValue = 1;
    }

    private void AddTagIfNotExists(string tag)
    {
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return; // Tag already exists
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
    }

    private List<string> ValidateRoomModule()
    {
        List<string> errors = new List<string>();

        // Check prefab
        if (roomModule.prefab == null)
        {
            errors.Add("No prefab assigned");
        }

        // Check dimensions
        if (roomModule.dimensions.x <= 0 || roomModule.dimensions.y <= 0 || roomModule.dimensions.z <= 0)
        {
            errors.Add("Invalid dimensions (must be positive)");
        }

        // Check difficulty range
        if (roomModule.difficultyRange[0] >= roomModule.difficultyRange[1])
        {
            errors.Add("Invalid difficulty range (min >= max)");
        }

        // Check sockets
        HashSet<RoomModule.Direction> directions = new HashSet<RoomModule.Direction>();
        foreach (var socket in roomModule.sockets)
        {
            if (!directions.Add(socket.direction))
            {
                errors.Add($"Duplicate socket direction: {socket.direction}");
            }

            if (string.IsNullOrEmpty(socket.id))
            {
                errors.Add("Socket with empty ID found");
            }
        }

        // Check for incompatible socket configurations
        foreach (var socket in roomModule.sockets)
        {
            if (socket.type == RoomModule.SocketType.Entrance && socket.direction == RoomModule.Direction.Up)
            {
                errors.Add("Entrance socket should not be on the Up direction");
            }
        }

        return errors;
    }

    [MenuItem("Tools/WFC/Create Room Module")]
    private static void CreateRoomModule()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Room Module",
            "NewRoomModule",
            "asset",
            "Create a new Room Module asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            RoomModule asset = CreateInstance<RoomModule>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }

    [MenuItem("Tools/WFC/Validate All Room Modules")]
    private static void ValidateAllRoomModules()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomModule");
        List<string> allErrors = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomModule module = AssetDatabase.LoadAssetAtPath<RoomModule>(path);

            if (module != null)
            {
                // Create a temporary editor to validate
                RoomModuleEditor editor = CreateInstance<RoomModuleEditor>();
                editor.target = module;
                editor.OnEnable();

                List<string> errors = editor.ValidateRoomModule();
                if (errors.Count > 0)
                {
                    allErrors.Add($"[{path}]: {string.Join(", ", errors)}");
                }
            }
        }

        if (allErrors.Count == 0)
        {
            Debug.Log("✓ All Room Modules validated successfully!");
        }
        else
        {
            Debug.LogError($"Found {allErrors.Count} validation errors:\n{string.Join("\n", allErrors)}");
        }
    }
}
