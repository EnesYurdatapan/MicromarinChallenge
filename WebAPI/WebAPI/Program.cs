using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Business;
using Business.Abstract;
using Business.Concrete;
using Core.Extensions;
using DataAccess;
using Microsoft.AspNetCore.Hosting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure Autofac container
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Servisleri Autofac ile kayýt ediyoruz ve AOP için Aspect ekliyoruz
    containerBuilder.RegisterType<ObjectDataService>()
        .As<IObjectDataService>()
        .EnableInterfaceInterceptors()
        .InterceptedBy(typeof(TransactionAspect));

  

    containerBuilder.RegisterType<TransactionAspect>().SingleInstance(); // Transaction Aspect'i kaydediyoruz
});
// Add services to the container.

builder.Services.AddBusinessServices();
builder.Services.AddDataAccessServices();

NpgsqlConnection.GlobalTypeMapper.UseJsonNet();


//builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


