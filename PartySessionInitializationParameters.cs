
namespace LouveSystems.K2.Lib
{
    [System.Serializable]
    public class PartySessionInitializationParameters
    {
        [System.Serializable]
        public class RealmToInitialize
        {
            public byte forPlayerId;
            public byte factionIndex;
            public byte? initialSubjugatorPlayerId;
            // Additional parameters... faction etc
        }

        public RealmToInitialize[] realmsToInitialize;

        public PartySessionInitializationParameters(params RealmToInitialize[] realmsToInitialize)
        {
            this.realmsToInitialize = realmsToInitialize;
        }
    }
}