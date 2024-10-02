using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entities
{
    public class Field : BaseEntity
    {
        // Bu alanın hangi şemaya ait olduğunu belirler (Foreign Key)
        public int ObjectSchemaId { get; set; }

        // Şema ile ilişki (Foreign Key)
        [JsonIgnore]
        public virtual ObjectSchema? ObjectSchema { get; set; }

        // Alanın ismi (örneğin: "Name", "Price" gibi)
        public string FieldName { get; set; }

        // Alanın veri tipi (örneğin: "string", "int", "decimal")
        public string FieldType { get; set; }

        // Alanın zorunlu olup olmadığını belirtir
        public bool IsRequired { get; set; }

        // Alanın uzunluğu (opsiyonel)
        public int? MaxLength { get; set; }

        // Alt şema (ilişkili başka bir tablo) varsa burada tanımlanacak
        public int? ChildSchemaId { get; set; }

        // Eğer bir alt şema varsa (relationship) buradan ilişkilendiriyoruz
        public virtual ObjectSchema? ChildSchema { get; set; }

        // Eğer bu alan bir foreign key ise, hedef tablonun adı tutulur
        public string? ForeignKeyTable { get; set; }
    }


}
