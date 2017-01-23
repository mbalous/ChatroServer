﻿using System.Data;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

[assembly: OwinStartup(typeof(ChatroServer.Startup))]

namespace ChatroServer
{
    public class Startup
    {
        private static readonly string DbName = Properties.Resources.DatabaseName;

        public void Configuration(IAppBuilder app)
        {
            // This method is executed once upon first request - it doesn't get automatically
            Debug.WriteLine($"Entered {nameof(Configuration)} method...");
            OrmLiteConnectionFactory dbFactory = ConfigureDatabase();

            HubConfiguration cfg = new HubConfiguration
            {
#if DEBUG
                EnableDetailedErrors = true

#else
                EnableDetailedErrors = false
#endif
            };

            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888
            Debug.WriteLine("Mapping SignalR...");
            app.MapSignalR(cfg);
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;
        }

        private static OrmLiteConnectionFactory ConfigureDatabase()
        {
            string connectionString = System.Configuration.ConfigurationManager.
                                            ConnectionStrings["MsSql"].ConnectionString;
            OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory(connectionString,
                new SqlServer2012OrmLiteDialectProvider());
            IDbConnection dbConnection = dbFactory.Open();
            dbConnection.ChangeDatabase("master");
            int result =
                dbConnection.SqlScalar<int>($"SELECT COUNT(*)FROM [master].sys.databases WHERE name = N'{DbName}'");
            if (result == 0)
            {
                Debug.WriteLine($"Database [{DbName}] not found. Creating database...");
                dbConnection.ExecuteSql($"CREATE DATABASE [{DbName}]");
            }
            dbConnection.ChangeDatabase(DbName);
            dbConnection.Close();
            return dbFactory;
        }
    }
}