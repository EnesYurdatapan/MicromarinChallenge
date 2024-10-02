using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using DataAccess.Concrete.EntityFramework.ObjectSchemaRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public static class ServiceRegistration
    {

        public static void AddDataAccessServices(this IServiceCollection services)
        {
            services.AddDbContext<Context>(options => options.UseNpgsql(Configuration.ConnectionString)); 
            services.AddScoped<IObjectSchemaReadRepository,ObjectSchemaReadRepository>();
            services.AddScoped<IObjectSchemaWriteRepository,ObjectSchemaWriteRepository>();

        }
    }
}
