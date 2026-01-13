using System;
using System.Data.SqlClient;

namespace Squash.Shared.Logging
{
    public static class Log4NetTableEnsurer
    {
        public static void Ensure(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID('dbo.Log4Net','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Log4Net](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [Date] [datetime2](7) NOT NULL,
        [Thread] [nvarchar](255) NOT NULL,
        [Level] [nvarchar](50) NOT NULL,
        [Logger] [nvarchar](255) NOT NULL,
        [Message] [nvarchar](max) NOT NULL,
        [Exception] [nvarchar](max) NULL,
     CONSTRAINT [PK_Log4Net] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END";

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
            }
            catch
            {
                // Keep startup resilient; logging will fail if table still missing.
            }
        }
    }
}
