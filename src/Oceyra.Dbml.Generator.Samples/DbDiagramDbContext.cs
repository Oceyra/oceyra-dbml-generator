using Microsoft.EntityFrameworkCore;
using Oceyra.Generator;

namespace Oceyra.Dbml.Generator.Samples;

[DbmlSource("schema/DbDiagram.dbml")]
public partial class DbDiagramDbContext(DbContextOptions<DbDiagramDbContext> options) : DbContext(options)
{ }