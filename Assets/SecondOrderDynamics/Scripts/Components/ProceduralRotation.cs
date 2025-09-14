using SecondOrderDynamics.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace SecondOrderDynamics.Scripts.Components {
    public class ProceduralRotation : SecondOrderSystemBase<Quaternion> {
        [SerializeField] private RotationMode rotationMode = RotationMode.Default;
        [SerializeField] private ProceduralMovement pM = null;
        [SerializeField] private float tiltAngle = 30f;
        [SerializeField] private float tF = 1, tZ = 0.5f, tR = 2f;
        [SerializeField] private float dirDead = 0.1f; // no normalize below this speed
        [SerializeField] private float dirSwitchSpeed = 1.0f; // must exceed this to allow 180° flips
        [SerializeField] private float dirSmoothingHz = 8f; // small low-pass on direction
        [SerializeField] private float maxSpeedForFullTilt = 10f; // speed where tilt reaches tiltAngle
    
        private SecondOrderSystem x, y, z, w, tx, tz;
        private Vector2 dir;

        protected override void Awake() {
            x = new SecondOrderSystem(f, base.z, r, target.rotation.x);
            y = new SecondOrderSystem(f, base.z, r, target.rotation.y);
            z = new SecondOrderSystem(f, base.z, r, target.rotation.z);
            w = new SecondOrderSystem(f, base.z, r, target.rotation.w);

            if (rotationMode == RotationMode.Velocity) {
                tx = new SecondOrderSystem(tF, tZ, tR, 0);
                tz = new SecondOrderSystem(tF, tZ, tR, 0);

                if (pM == null)
                    pM = GetComponentInParent<ProceduralMovement>();

                systems = new[] { x, y, z, w, tx, tz };
            }
            else {
                systems = new[] { x, y, z, w };
            }
        }

        protected override Quaternion Calculate(float t, Quaternion x) {
            Quaternion y = new(this.x.currentValue, this.y.currentValue, z.currentValue, w.currentValue);
            if (Quaternion.Dot(x, y) < 0f) x = ScalarMul(x, -1f);

            var nx = this.x.Update(t, x.x);
            var ny = this.y.Update(t, x.y);
            var nz = z.Update(t, x.z);
            var nw = w.Update(t, x.w);

            var result = new Quaternion(nx, ny, nz, nw);

            if (rotationMode == RotationMode.Velocity && pM) {
                var vWorld = new Vector3(pM.x.currentVelocity, pM.y.currentVelocity, pM.z.currentVelocity);
                var vLocal = transform.InverseTransformDirection(vWorld);

                var v2 = new Vector2(vLocal.x, vLocal.z);
                var speed = v2.magnitude;
            
                var desiredDir = speed > dirDead ? (v2 / speed) : Vector2.zero;
            
                if (dir == Vector2.zero) dir = desiredDir;
            
                var wantsFlip = Vector2.Dot(dir, desiredDir) < 0f;
            
                if (!wantsFlip || speed > dirSwitchSpeed) {
                    var a = 1f - Mathf.Exp(-dirSmoothingHz * t);
                    dir = Vector2.Lerp(dir, desiredDir, a);
                }

                var k = Mathf.InverseLerp(dirDead, maxSpeedForFullTilt, speed);

                var xTilt = tx.Update(t, dir.y * tiltAngle * k);
                var zTilt = tz.Update(t, dir.x * tiltAngle * k);

                xTilt = Mathf.Clamp(xTilt, -tiltAngle, tiltAngle);
                zTilt = Mathf.Clamp(zTilt, -tiltAngle, tiltAngle);

                result *= Quaternion.Euler(xTilt, 0f, zTilt);
            }

            return result.normalized;
        }

        protected override void Execute(float t) {
            if (target) {
                transform.rotation = Calculate(t, target.rotation);
            }
        }

        private static Quaternion ScalarMul(Quaternion q, float scalar) =>
            new(q.x * scalar, q.y * scalar, q.z * scalar, q.w * scalar);
    }


    [CustomEditor(typeof(ProceduralRotation))]
    public class ProceduralRotationEditor : SecondOrderSystemBaseEditor {
        private SerializedProperty rotationMode;
        private SerializedProperty pM;
        private SerializedProperty tiltAngle, tF, tZ, tR;
        private SerializedProperty dirDead, dirSwitchSpeed, dirSmoothingHz, maxSpeedForFullTilt;

        protected override void OnEnable() {
            base.OnEnable();

            rotationMode = serializedObject.FindProperty("rotationMode");
            pM = serializedObject.FindProperty("pM");
            tF = serializedObject.FindProperty("tF");
            tZ = serializedObject.FindProperty("tZ");
            tR = serializedObject.FindProperty("tR");
            tiltAngle = serializedObject.FindProperty("tiltAngle");

            dirDead = serializedObject.FindProperty("dirDead");
            dirSwitchSpeed = serializedObject.FindProperty("dirSwitchSpeed");
            dirSmoothingHz = serializedObject.FindProperty("dirSmoothingHz");
            maxSpeedForFullTilt = serializedObject.FindProperty("maxSpeedForFullTilt");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.PropertyField(rotationMode);

            if ((RotationMode)rotationMode.enumValueIndex == RotationMode.Velocity) {
                EditorGUILayout.PropertyField(pM);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Tilt Springs", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(tF);
                EditorGUILayout.PropertyField(tZ);
                EditorGUILayout.PropertyField(tR);
                EditorGUILayout.PropertyField(tiltAngle);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Velocity Direction Filtering", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(dirDead, new GUIContent("Dead Speed"));
                EditorGUILayout.PropertyField(dirSwitchSpeed, new GUIContent("Flip Threshold Speed"));
                EditorGUILayout.PropertyField(dirSmoothingHz, new GUIContent("Direction Smoothing (Hz)"));
                EditorGUILayout.PropertyField(maxSpeedForFullTilt, new GUIContent("Max Speed for Full Tilt"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void UpdateConstants() {
            var targetObject = (SecondOrderSystemBase<Quaternion>)target;

            for (var i = 0; i < targetObject.systems.Length; i++) {
                var system = targetObject.systems[i];
                if (i != targetObject.systems.Length - 1 && i != targetObject.systems.Length - 2)
                    system.UpdateConstants(f.floatValue, z.floatValue, r.floatValue);
                else
                    system.UpdateConstants(tF.floatValue, tZ.floatValue, tR.floatValue);
            }
        }
    }

    public enum RotationMode {
        Default,
        Velocity
    }
}