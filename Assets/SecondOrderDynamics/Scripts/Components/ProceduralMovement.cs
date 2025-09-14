using UnityEditor;
using UnityEngine;

public class ProceduralMovement : SecondOrderSystemBase<Vector3> {
    public SecondOrderSystem x, y, z;

    protected override void Awake() {
        x = new SecondOrderSystem(f, base.z, r, transform.position.x);
        y = new SecondOrderSystem(f, base.z, r, transform.position.y);
        z = new SecondOrderSystem(f, base.z, r, transform.position.z);

        systems = new[] { x, y, z };
    }

    protected override Vector3 Calculate(float t, Vector3 x) {
        return new Vector3(this.x.Update(t, x.x), y.Update(t, x.y), z.Update(t, x.z));
    }

    // implement tilting towards target
    protected override void Execute(float t) {
        if (target != null && !(!IsMoving() && IsApproximatelyEqualTo(transform.position, target.position))) {
            transform.position = Calculate(t, target.position);
        }
    }

    private bool IsMoving() {
        return x.currentVelocity != 0 || y.currentVelocity != 0 || z.currentVelocity != 0;
    }
}

[CustomEditor(typeof(ProceduralMovement))]
public class ProceduralMovementEditor : SecondOrderSystemBaseEditor {
    protected override void UpdateConstants() {
        var targetObject = (SecondOrderSystemBase<Vector3>)target;
        foreach (var system in targetObject.systems) {
            system.UpdateConstants(f.floatValue, z.floatValue, r.floatValue);
        }
    }
}
