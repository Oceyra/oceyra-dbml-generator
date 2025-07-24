using Microsoft.EntityFrameworkCore;
using Oceyra.Generator;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oceyra.Dbml.Generator.Samples;

[DbmlSource("schema/TaskManagerDb.dbml")]
public partial class TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : DbContext(options)
{ }