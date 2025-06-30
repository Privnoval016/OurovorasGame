using UnityEngine;

public interface IContactDetector
{
    public abstract Vector3 GetClosestPointOnCollider(Collider col);
}
