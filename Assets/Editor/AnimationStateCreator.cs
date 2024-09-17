using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimationStateCreator : EditorWindow
{
    private string controllerPath = "Assets/MyAnimatorController.controller";

    [MenuItem("Tools/Create Animation States")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AnimationStateCreator));
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);

        if (GUILayout.Button("Create Animation States"))
        {
            CreateAnimationStates();
        }
    }

    void CreateAnimationStates()
    {
        // Load the Animator Controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("Animator Controller not found.");
            return;
        }

        // Load all animation clips from the Resources folder
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("");
        foreach (AnimationClip clip in clips)
        {
            // Create a new state in the Animator Controller for each clip
            var state = controller.layers[0].stateMachine.AddState(clip.name);
            state.motion = clip;
        }

        Debug.Log("Animation states created for all clips in Resources folder.");
    }
}
