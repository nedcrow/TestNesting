using UnityEngine;

namespace Hanok
{
    public class BasicPoolingComponent : PoolingComponent<Transform>
    {
        [SerializeField] private BasicGameObjectCatalog basicCatalog;

        public BasicGameObjectCatalog BasicCatalog => basicCatalog;

        protected override Catalog GetCatalog() => basicCatalog;
        protected override string GetComponentTypeName() => "HousePoolingComponent";

        public GameObject GetGameObject(GameObject gameObject) => Get(gameObject.transform);
        public void ReturnGameobject(GameObject gameObject) => Return(gameObject);
    }

}