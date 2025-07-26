using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oceyra.Dbml.Generator.Samples;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<TaskManagerDbContext>(options=> options.UseSqlite("TaskManagerDb.db"));
builder.Services.AddDbContext<DbDiagramDbContext>(options => options.UseSqlite("DbDiagramDb.db"));
builder.Services.AddDbContext<CompositeKeyDbContext>(options => options.UseSqlite("CompositeKeyDb.db"));

await builder.Build().RunAsync();