using UnityEditor;
using UnityEngine;

public abstract class SecondOrderSystemBase<T> : MonoBehaviour {
    [SerializeField] protected float f = 1; // natural frequency of a resonant system .. speed of responsiveness 
    [SerializeField] protected float z = 0.5f; // damping coefficient .. how the system comes to settle at the target
    [SerializeField] protected float r = 2; // initial response
    [SerializeField] protected UpdateMode updateMode = UpdateMode.Update;
    public Transform target = null;

    [HideInInspector] public SecondOrderSystem[] systems;

    protected abstract void Awake();

    protected virtual void Update() {
        if (updateMode == UpdateMode.Update) Execute(Time.deltaTime);
    }

    protected virtual void FixedUpdate() {
        if (updateMode == UpdateMode.FixedUpdate) Execute(Time.fixedDeltaTime);
    }

    protected virtual void LateUpdate() {
        if (updateMode == UpdateMode.LateUpdate) Execute(Time.deltaTime);
    }

    protected abstract void Execute(float t);

    protected abstract T Calculate(float t, T x);

    protected static bool IsApproximatelyEqualTo(Vector3 a, Vector3 b, float epsilon = 0.1f) =>
        Vector3.SqrMagnitude(a - b) < epsilon * epsilon;
}

[CustomEditor(typeof(SecondOrderSystemBase<>), true)]
public abstract class SecondOrderSystemBaseEditor : Editor {
    protected SerializedProperty f, z, r;
    private SerializedProperty updateModeProperty;
    private SerializedProperty targetProperty;

    private float previousF;
    private float previousZ;
    private float previousR;

    protected virtual void OnEnable() {
        f = serializedObject.FindProperty("f");
        z = serializedObject.FindProperty("z");
        r = serializedObject.FindProperty("r");
        updateModeProperty = serializedObject.FindProperty("updateMode");
        targetProperty = serializedObject.FindProperty("target");

        previousF = f.floatValue;
        previousZ = z.floatValue;
        previousR = r.floatValue;
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(f);
        EditorGUILayout.PropertyField(z);
        EditorGUILayout.PropertyField(r);
        EditorGUILayout.PropertyField(updateModeProperty);
        EditorGUILayout.PropertyField(targetProperty);

        if (Application.isPlaying) {
            if (!Mathf.Approximately(f.floatValue, previousF) || !Mathf.Approximately(z.floatValue, previousZ) ||
                !Mathf.Approximately(r.floatValue, previousR)) {
                previousF = f.floatValue;
                previousZ = z.floatValue;
                previousR = r.floatValue;

                UpdateConstants();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    protected abstract void UpdateConstants();
}