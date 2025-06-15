namespace Identity.API.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UsersCollectionName { get; set; } = "Users";
    public string RolesCollectionName { get; set; } = "Roles";
    public string ClientsCollectionName { get; set; } = "Clients";
    public string IdentityResourcesCollectionName { get; set; } = "IdentityResources";
    public string ApiResourcesCollectionName { get; set; } = "ApiResources";
    public string ApiScopesCollectionName { get; set; } = "ApiScopes";
    public string PersistedGrantsCollectionName { get; set; } = "PersistedGrants";
    public string DeviceFlowCodesCollectionName { get; set; } = "DeviceFlowCodes";
}

public static class MongoDbConfig
{
    public static void RegisterClassMaps()
    {
        // Ensure class maps are registered only once
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(ApplicationUser)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<ApplicationUser>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(ApplicationRole)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<ApplicationRole>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
