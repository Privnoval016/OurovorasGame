using System;
using Extensions.Persistence;
using Extensions.Serialization;
using UnityEngine;


[Serializable]
public class PlayerSaveData : SaveableBase<PlayerSaveBinding>
{
    [field: SerializeField] public SerializableGuid Id { get; set; }
    public Vector3 position;
    public Quaternion rotation;
}