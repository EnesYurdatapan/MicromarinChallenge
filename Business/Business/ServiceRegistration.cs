using Business.Abstract;
using Business.Concrete;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public static class ServiceRegistration
    {
        public static void AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IObjectSchemaService,ObjectSchemaService>();
            services.AddScoped<IObjectDataService,ObjectDataService>();
        }
    }
}
