using Microsoft.EntityFrameworkCore;
using Oceyra.Generator;

namespace Oceyra.Dbml.Generator.Samples;

[DbmlSource("schema/CompositeKeyDb.dbml")]
public partial class CompositeKeyDbContext(DbContextOptions<CompositeKeyDbContext> options) : DbContext(options)
{ }