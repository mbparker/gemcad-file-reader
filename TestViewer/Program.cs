using Autofac;
using Autofac.Extensions.DependencyInjection;
using LibGemcadFileReader;
using LibGemcadFileReader.Abstract;
using TestViewer;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add services to the container.
builder.Host.ConfigureContainer<ContainerBuilder>(contBldr =>
{
    contBldr.RegisterModule<ContainerModuleRegistration>();
    contBldr.RegisterType<ViewerLogger>().As<ILoggerService>().SingleInstance();
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");
;

app.Run();