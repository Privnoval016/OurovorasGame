using System;
using Extensions.Persistence;
using Extensions.Serialization;
using UnityEngine;

public class PlayerSaveBinding : MonoBehaviour, IBind<PlayerSaveData>
{
    [HideInInspector] public PlayerController pc;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    [field: SerializeField] public SerializableGuid Id { get; set; } = SerializableGuid.NewGuid();
    [SerializeField] private PlayerSaveData data;

    public void Bind(PlayerSaveData newData)
    {
        data = newData;
        data.Id = Id;
        transform.position = data.position;
        transform.rotation = data.rotation;
    }

    private void Update()
    {
        if (data != null)
        {
            data.position = transform.position;
            data.rotation = transform.rotation;
        }
    }
}