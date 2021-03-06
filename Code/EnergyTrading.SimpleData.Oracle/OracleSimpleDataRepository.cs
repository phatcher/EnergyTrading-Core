﻿namespace EnergyTrading.SimpleData.Oracle
{
    using EnergyTrading.Configuration;
    using EnergyTrading.Data.SimpleData;

    using Simple.Data;
    using Simple.Data.Oracle;

    public abstract class OracleSimpleDataRepository : SimpleDataRepository
    {
        protected OracleSimpleDataRepository(Database database, string schema = "", int maxRetries = 3, SimpleDataMode mode = SimpleDataMode.Live) : base(database, schema, maxRetries, mode)
        {
        }

        protected OracleSimpleDataRepository(IConfigurationManager configurationManager, string connectionName = SimpleDataDatabaseProvider.DefaultConnectionName, string schema = "", int maxRetries = 3, SimpleDataMode mode = SimpleDataMode.Live)
            : base(configurationManager, connectionName, schema, maxRetries, mode)
        {
        }

        protected Sequence GetNextIdInsertValue()
        {
            return Sequence.Next(this.GetDottedSchema() + this.SequenceName);
        }

        protected Sequence GetCurrentIdInsertValue()
        {
            return Sequence.Current(this.GetDottedSchema() + this.SequenceName);
        }
    }
}