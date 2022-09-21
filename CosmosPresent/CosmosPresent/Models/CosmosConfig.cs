namespace CosmosDemo.Models
{
    public static class CosmosConfig
    {
        // ADD THIS PART TO YOUR CODE

        // The Azure Cosmos DB endpoint for running this sample.
        public static readonly string ConnectionString = "AccountEndpoint=https://tfs-cosmos-dev.documents.azure.com:443/;AccountKey=Vt784lKyD4BaEDqBiLtEUKQjw7OjlywCMwmaOQNnjLg7Ikr2Z9o1cBSpKshtWRQHWmMhp9ztP8Hkl8ndfEhizw==";
        // The primary key for the Azure Cosmos account.
        public static readonly string DatabaseName = "cosmos-dev";

        public static readonly string Audit = "audit";
        public static readonly string User = "user";
    }
}
