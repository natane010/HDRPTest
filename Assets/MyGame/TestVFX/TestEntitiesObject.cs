using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;

namespace TK.Entities.Test
{
    [System.Serializable, GenerateAuthoringComponent, BurstCompile]
    public struct TestEntitiesObject : IComponentData
    {
        public Entity prefab;
    }
}
