using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileConversion : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject bulletImpact;
    public float timeToLive;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(bulletImpact);
    }

    public void Convert(Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem)
    {
        manager.AddComponent(entity, typeof(ProjectileData));

        ProjectileData visualEffect = new ProjectileData { hitEffect = conversionSystem.GetPrimaryEntity(bulletImpact), timeToLive_Effect = this.timeToLive };
        TimeToLive time = new TimeToLive { Value = timeToLive };

        manager.AddComponentData(entity, visualEffect);

    }
}