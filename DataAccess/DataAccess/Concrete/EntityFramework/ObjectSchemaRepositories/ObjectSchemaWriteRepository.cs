using DataAccess.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.ObjectSchemaRepositories
{
    public class ObjectSchemaWriteRepository : WriteRepository<ObjectSchema>, IObjectSchemaWriteRepository
    {
        public ObjectSchemaWriteRepository(Context context) : base(context)
        {
        }
    }
}
