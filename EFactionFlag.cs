
namespace LouveSystems.K2.Lib
{
    public enum EFactionFlag
    {
        None = 0,

        Charge = 1 << 0,
        ConquestBuilding = 1 << 1,
        GetMoneyFromNeighborBuildings = 1 << 2,
        FortsCountAsCapital = 1 << 3,
        RicherTerritories = 1 << 4,
        LootMoreMoney = 1 << 5,
        ConqueredFortsGivePayout = 1 << 6,
        SeeEnemyPlannedConstructions = 1 << 7,
        Subjugate = 1 << 8,
    }
}