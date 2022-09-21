using CosmosDemo.Models;
using CosmosPresent.Models.Container;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using User = CosmosPresent.Models.Container.User;

namespace CosmosPresent
{
    class Program
    {

        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = "https://localhost:8081";
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        // The Cosmos client instance
        private CosmosClient _cosmosClient;

        // The database we will create
        private Database _database;

        // The container we will create.
        private Container _container;

        // The name of the database and container we will create
        private string databaseId = "Demo";
        private string containerId = "Audit";
        private string userContainerId = "User";

        public Program()
        {

        }
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.GetStartedDemoAsync();

            }
            catch (CosmosException cosmosException)
            {
                Console.WriteLine("Cosmos Exception with Status {0} : {1}\n", cosmosException.StatusCode, cosmosException);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this._cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            // await TestIndexMetrics();
            // await TestHotPartitioinKeyMetrics();
            // await QueryWithContinuationTokens();
            // await this.AddItemsToContainerAsync();
        }

        #region

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this._database = await this._cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this._database.Id);
        }
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this._container = await this._database.CreateContainerIfNotExistsAsync(userContainerId, "/PartitionKey");
            Console.WriteLine("Created Container: {0}\n", this._container.Id);
        }

        public List<User> GetListAudit()
        {
            var dateTimeNow = DateTime.Now;
            List<User> audits = new List<User>();
            for (int i = 0; i < 2; i++)
            {
                audits.Add(new User
                {
                    Id = $"{Guid.NewGuid().ToString()}",
                    Document = new Document { Name = "Document 1" },
                    FileType = new File[]
                    {
                        new File{Name =$"File {i}",Type = FileType.CSV},
                        new File{Name =$"File {i}",Type = FileType.PDF}
                    },
                    Type = "Document",
                    DateCreated = DateTime.Now,
                    Tenant = "RBCHSBC",
                    PartitionKey = Audit.GetPartitionKey("RBCHSBC", dateTimeNow)
                });

            }

            return audits;
        }

        private async Task AddItemsToContainerAsync()
        {
            var audits = GetListAudit();
            foreach (var audit in audits)
            {
                //try
                //{
                //    // Read the item to see if it exists.  
                //    ItemResponse<Audit> andersenFamilyResponse = await this._container.ReadItemAsync<Audit>(audit.Id,new PartitionKey(Audit.GetPartitionKey(audit.Tenant,audit.DateCreated)));
                //    Console.WriteLine("Item in database with id: {0} already exists\n", andersenFamilyResponse.Resource.Id);
                //}
                //catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                //{
                ItemResponse<User> andersenFamilyResponse = await this._container.CreateItemAsync<User>(audit, new PartitionKey($"{audit.Tenant}-{audit.DateCreated.ToString("yyyy-MM")}"));
                //await Task.Delay(500);
                //    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", andersenFamilyResponse.Resource.Id, andersenFamilyResponse.RequestCharge);
                //}
            }
        }

        #endregion

        // cross
        // retail => catalog data,  azure function trigger
        // consitency 
        // transac/concurrency


        public async Task TestIndexMetrics()
        {
            //string lowIndex = "SELECT top 500 * FROM c where c.Tenant = 'RBCHSBC' and IS_Defined(c.DateCreated)";

            //string suggestCompositeIndex = "SELECT top 20000 * FROM c where  c.PartitionKey ='RBCHSBC-2022-08' and c.Tenant ='RBCHSBC' and c._ts > 1661422201";
            string appliedCompositeIndex = "SELECT top 20000 * FROM c where  c.PartitionKey ='RBCHSBC-2022-08' and c.Tenant ='RBCHSBC' and c._ts > 1661422201  order by c.PartitionKey, c.Tenant, c._ts asc";

            QueryDefinition query = new QueryDefinition(appliedCompositeIndex);

            FeedIterator<Models.Container.User> feedIterator = _container.GetItemQueryIterator<Models.Container.User>(
                query, requestOptions: new QueryRequestOptions
                {
                    PopulateIndexMetrics = true,
                    MaxItemCount = -1
                });

            double requestCharge = 0;
            FeedResponse<Models.Container.User> response = null;
            List<Models.Container.User> auditModels = new List<Models.Container.User>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            while (feedIterator.HasMoreResults)
            {
                response = await feedIterator.ReadNextAsync();
                requestCharge += response.RequestCharge;

                auditModels.AddRange(response.ToList());
                Console.WriteLine($"Index metris: " + response.IndexMetrics);
                Console.WriteLine($"-------------------------");
            }
            stopwatch.Stop();
            var ts = stopwatch.Elapsed;

            Console.WriteLine($"Total Request Units consumed: { requestCharge} RUs");
            Console.WriteLine($"Total Query returned: { auditModels.Count} results");
            Console.WriteLine($"Total time: {string.Format("{0:00}:{1:00}:{2:00}:{3:00}", stopwatch.Elapsed.Hours, ts.TotalMinutes, ts.TotalSeconds, ts.TotalMilliseconds)}");
        }

        public async Task TestHotPartitioinKeyMetrics()
        {
            try
            {
                // Canso
                var cansoQuery = "SELECT * FROM c where c.PartitionKey ='CANSO-2022-08'";

                // RBCHSBC
                var rbchsbcQuery = "SELECT * FROM c where c.PartitionKey ='RBCHSBC-2022-08'";

                var tenTask = new List<int>();
                for (int i = 0; i < 10; i++)
                {
                    tenTask.Add(i);
                }

                Parallel.ForEach(tenTask, parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = 1000 }, async item =>
                {
                    await Task.WhenAll(GetUsers(cansoQuery), GetUsers(rbchsbcQuery));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public  async Task QueryWithContinuationTokens()
        {
            QueryDefinition query = new QueryDefinition("SELECT top 10 * FROM c where c.PartitionKey ='CANSO-2022-08' order by c._ts desc");
            string continuation = null;

            List<User> results = new List<User>();
            using (FeedIterator<User> resultSetIterator = _container.GetItemQueryIterator<User>(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    MaxItemCount = 1
                }))
            {
                // Execute query and get 1 item in the results. Then, get a continuation token to resume later
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<User> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }

                    // Get continuation token once we've gotten > 0 results. 
                    if (response.Count > 0)
                    {
                        continuation = response.ContinuationToken;
                        break;
                    }
                }
            }

            // Check if query has already been fully drained
            if (continuation == null)
            {
                return;
            }

            // Resume query using continuation token
            using (FeedIterator<User> resultSetIterator = _container.GetItemQueryIterator<User>(
                    query,
                    requestOptions: new QueryRequestOptions()
                    {
                        MaxItemCount = -1
                    },
                    continuationToken: continuation))
            {
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<User> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }
                }
            }
        }

        private async Task<List<Models.Container.User>> GetUsers(string query)
        {
            try
            {
                QueryDefinition queryDefinition = new QueryDefinition(query);
                FeedIterator<Models.Container.User> feedIterator = _container.GetItemQueryIterator<Models.Container.User>(
                  queryDefinition, requestOptions: new QueryRequestOptions
                  {
                      PopulateIndexMetrics = true,
                      MaxItemCount = -1
                  });

                FeedResponse<Models.Container.User> response = null;

                double requestCharge = 0;
                List<Models.Container.User> users = new List<Models.Container.User>();
                while (feedIterator.HasMoreResults)
                {
                    response = await feedIterator.ReadNextAsync();
                    requestCharge += response.RequestCharge;
                    users.AddRange(response.ToList());
                }

                Console.WriteLine($"Total Request Units consumed: { requestCharge} RUs");
                Console.WriteLine($"Total Query returned: { users.Count} results");

                return users;

            }
            catch (Exception ex)
            {
                Console.WriteLine(query);
                Console.WriteLine(ex.Message);
                return null;
                throw;
            }
        }
    }

}