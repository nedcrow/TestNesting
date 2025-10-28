using UnityEngine;

namespace Hanok
{
    public class HousePoolingComponent : PoolingComponent<House>
    {
        [SerializeField] private HouseCatalog houseCatalog;

        public HouseCatalog HouseCatalog => houseCatalog;

        protected override Catalog GetCatalog() => houseCatalog;
        protected override string GetComponentTypeName() => "HousePoolingComponent";

        public GameObject GetHouse(House houseType) => Get(houseType);
        public void ReturnHouse(GameObject houseObject) => Return(houseObject);
    }
}