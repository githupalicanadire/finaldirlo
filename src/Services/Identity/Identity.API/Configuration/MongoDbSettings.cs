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
