using Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTOs
{
    public class AddObjectSchemaDTO
    {
        // Objelerin ismi (örneğin: "Customer", "Invoice" gibi)
        public string ObjectType { get; set; }

        // Bu obje tipi için tanımlı olan field'lar (ilişkili alanlar)
        public virtual ICollection<Field> Fields { get; set; } = new List<Field>();
    }
}
