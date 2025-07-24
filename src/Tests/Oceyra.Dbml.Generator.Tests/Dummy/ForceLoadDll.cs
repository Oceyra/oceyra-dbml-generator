using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oceyra.Dbml.Generator.Tests.Dummy
{
    [Table("ForceLoadDll")]
    [Index(nameof(Id), Name = "public_idx_ForceLoadDll_Id", IsUnique = true)]
    public class ForceLoadDll
    {
        [Key]
        [Required]
        [Column("Id")]
        public int Id { get; set; }
    }
}
