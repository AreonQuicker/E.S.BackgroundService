using E.S.Data.Query.DataAccess.Interfaces;

namespace E.S.BackgroundService;

internal static class AppExtensions
{
    internal static async Task CreateSchemaAndTableAsync(this IDataAccessQuery dataAccessQuery)
    {
        if (dataAccessQuery != null)
        {
            var schemaCount = await dataAccessQuery.FirstOrDefaultQueryAsync<int>(
                "SELECT COUNT(*) FROM sys.schemas WHERE name = 'BackgroundService'");

            if (schemaCount == 0) await dataAccessQuery.FirstOrDefaultQueryAsync<dynamic>("CREATE SCHEMA BackgroundService");

            var tableCount = await dataAccessQuery.FirstOrDefaultQueryAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'BackgroundService' AND TABLE_NAME = 'CronJob'");

            if (tableCount == 0)
                await dataAccessQuery.FirstOrDefaultQueryAsync<dynamic>(
                    "CREATE TABLE [BackgroundService].[CronJob] ( [Id] [INT] IDENTITY(1, 1) NOT NULL, [CreatedDate] [DATETIME] NOT NULL, [Year] [INT] NOT NULL, [Month] [INT] NOT NULL, [Day] [INT] NOT NULL, [Job] VARCHAR(255) NOT NULL, CONSTRAINT [PK_CronJob] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON ) ON [PRIMARY] ) ON [PRIMARY]");
        }
    }
}