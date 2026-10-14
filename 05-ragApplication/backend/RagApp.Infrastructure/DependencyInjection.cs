using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RagApp.Application;

namespace RagApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<RagDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));

            services.AddScoped<IVectorStore, EfVectorStore>();
            services.AddScoped<IRagService, RagService>();

            return services;
        }
    }
}
