using System;
using System.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;


namespace ChatroServer
{
    public class SingletonDbConnection
    {
        private static readonly Lazy<SingletonDbConnection> Lazy =
            new Lazy<SingletonDbConnection>(() => new SingletonDbConnection());

        public OrmLiteConnectionFactory ConnectionFactory;

        public static SingletonDbConnection Instance => Lazy.Value;

        private SingletonDbConnection()
        {
            string connectionString =
                ConfigurationManager.ConnectionStrings["MsSql"].ConnectionString;
           connectionString = connectionString.Replace("master", ChatroServer.Properties.Resources.DatabaseName);
            this.ConnectionFactory = new OrmLiteConnectionFactory(connectionString,
                new SqlServer2012OrmLiteDialectProvider());
        }
    }
}