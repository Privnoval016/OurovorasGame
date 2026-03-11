using System.Collections.Generic;
using UnityEngine;

public interface IVFXSpawnLocation
{
    Transform GetSpawnTransform();
}

public interface IVFXSpawnLocationOwner
{
    List<IVFXSpawnLocation> GetVFXSpawnLocations();
    Transform GetTransform();
}