using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace redis_com_client
{
    public class CacheFactory
    {
        public CacheFactory()
        {
        }

        private bool _useAdminConnection = false;

        public bool UseAdminConnection
        {
            get
            {
                return _useAdminConnection;
            }
            set
            {
                _useAdminConnection = value;
                _connectionMultiplexer = null;
            }
        }

        public IDatabase Instance
        {
            get
            {
                return GetConnectionMultiplexer().GetDatabase();
            }
        }

        public IEnumerable<IServer> Servers
        {
            get
            {
                return GetConnectionMultiplexer().GetEndPoints().Select(x => _connectionMultiplexer.GetServer(x));
            }
        }

        private static object _lockObject = new object();

        private static ConnectionMultiplexer _connectionMultiplexer;

        private ConnectionMultiplexer GetConnectionMultiplexer()
        {
            if (_connectionMultiplexer == null)
            {
                var connectionString = System.Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost";
                if (_useAdminConnection)
                {
                    connectionString += ",allowAdmin=true";
                }
                lock (_lockObject)
                {
                    _connectionMultiplexer =
                        _connectionMultiplexer ??
                        ConnectionMultiplexer.Connect(connectionString);
                }
            }
            return _connectionMultiplexer;
        }
    }
}