using UnityEngine;
using System;

public class SecondOrderSystem {
    private float k1, k2, k3, k1Stable, k2Stable;
    private float w, z, d;

    private float previousTarget;
    public float currentValue, currentVelocity;

    private const float PI = Mathf.PI;

    public SecondOrderSystem(float f, float z, float r, float x0) {
        UpdateConstants(f, z, r);

        previousTarget = x0;
        currentValue = x0;
        currentVelocity = 0;
    }

    public void UpdateConstants(float f, float z, float r) {
        w = 2 * PI * f;
        this.z = z;

        d = w * Mathf.Sqrt(Mathf.Max(z * z - 1, 0));

        k1 = z / (PI * f);
        k2 = 1 / (w * w);
        k3 = r * z / w;
    }

    public float Update(float t, float x, float? xd = null) {
        if (xd == null) {
            xd = (x - previousTarget) / t;
            previousTarget = x;
        }

        if (w * t < z) { // clamp k2 to guarantee stability without jitter
            k1Stable = k1;
            k2Stable = Mathf.Max(k2, t * t / 2 + t * k1 / 2, t * k1);
        }
        else { // use pole zero matching if the system is very fast
            var t1 = Mathf.Exp(-z * w * t);
            var alpha = 2 * t1 * (z <= 1 ? Mathf.Cos(t * d) : (float)Math.Cosh(t * d));
            var beta = t1 * t1;
            var t2 = t / (1 + beta - alpha);
            
            k1Stable = (1 - beta) * t2;
            k2Stable = t * t2;
        }

        currentValue += t * currentVelocity;
        currentVelocity += t * (x + k3 * xd.Value - currentValue - k1Stable * currentVelocity) / k2Stable;

        return currentValue;
    }
}
