using Microsoft.Extensions.Configuration;

namespace ResolveAi.Api.Data
{
    public class ConexaoResolveAi
    {
        public string ConnectionString { get; }

        public ConexaoResolveAi(IConfiguration configuration)
        {
            var db = configuration.GetSection("Database");

            var port = db["Port"] ?? "3306";
            var ssl = db["SslMode"] ?? "None";

            ConnectionString =
                $"Server={db["Server"]};" +
                $"Port={port};" +
                $"Database={db["Name"]};" +
                $"User ID={db["User"]};" +
                $"Password={db["Password"]};" +
                $"SslMode={ssl};";
        }
    }
}
