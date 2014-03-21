using System;
using System.Configuration;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;

namespace Tweets.Repositories
{
    public abstract class SqlRepositoryBase
    {
        private readonly string connectionString;
        private readonly AttributeMappingSource mappingSource;

        protected SqlRepositoryBase()
        {
            mappingSource = new AttributeMappingSource();
            connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;            
        }

        protected void UseDatabase(Action<DataContext> task)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var dataContext = new DataContext(connection, mappingSource);
                task(dataContext);
                dataContext.SubmitChanges();
            }
        }
    }
}