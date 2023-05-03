namespace CatScale.ReprocessTool;

public class Config
{
    public InfluxDbConfig InfluxDb { get; }
    public ServiceConfig Service { get; }

    public Config(InfluxDbConfig influxDb, ServiceConfig service)
    {
        InfluxDb = influxDb ?? throw new ArgumentException($"Missing config section {nameof(InfluxDb)}");
        Service = service ?? throw new ArgumentException($"Missing config section {nameof(Service)}");
    }
}

public class InfluxDbConfig
{
    public string Uri { get; }
    public string Token { get; }
    public string Org { get; }
    public string Bucket { get; }

    public InfluxDbConfig(string uri, string token, string org, string bucket)
    {
        if (String.IsNullOrWhiteSpace(uri))
            throw new ArgumentException($"Missing config {nameof(InfluxDbConfig)}.{nameof(Uri)}");
        if (String.IsNullOrWhiteSpace(token))
            throw new ArgumentException($"Missing config {nameof(InfluxDbConfig)}.{nameof(Token)}");
        if (String.IsNullOrWhiteSpace(org))
            throw new ArgumentException($"Missing config {nameof(InfluxDbConfig)}.{nameof(Org)}");
        if (String.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException($"Missing config {nameof(InfluxDbConfig)}.{nameof(Bucket)}");
        
        Uri = uri;
        Token = token;
        Org = org;
        Bucket = bucket;
    }
}

public class ServiceConfig
{
    public string Uri { get; }
    public string Token { get; }
    
    public ServiceConfig(string uri, string token)
    {
        if (String.IsNullOrWhiteSpace(uri))
            throw new ArgumentException($"Missing config {nameof(ServiceConfig)}.{nameof(Uri)}");
        if (String.IsNullOrWhiteSpace(token))
            throw new ArgumentException($"Missing config {nameof(ServiceConfig)}.{nameof(Token)}");

        Uri = uri;
        Token = token;
    }
}