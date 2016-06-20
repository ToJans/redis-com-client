using System;
using System.EnterpriseServices;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace redis_com_client
{
    [ComVisible(true)]
    [Guid("F9BC3566-FD7D-40E5-8DAA-61CBB179DE05")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CacheManager")]
    [Synchronization(SynchronizationOption.Disabled)]
    public class CacheManager : ICacheManager
    {
        private string _storePrefix;

        public TimeSpan DefaultLifeTime { get; set; } = TimeSpan.FromMinutes(15);

        public bool IsExtendingLifeTimeUponGet { get; set; } = true; 

        public CacheManager()
        {
        }

        public void SetExpiration(string key, TimeSpan lifeTime)
        {
            CacheFactory.GetInstance().KeyExpire(GenerateFullKey(key), lifeTime);
        }

        public void RemoveAll()
        {
            var mask = $"{_storePrefix}*";
            CacheFactory.GetInstance().ScriptEvaluate("local keys = redis.call('keys', ARGV[1]) for i=1,#keys,5000 do redis.call('del', unpack(keys, i, math.min(i+4999, #keys))) end return keys", null, new RedisValue[] { mask });
        }

        public object Get(string key)
        {
            var fullKey = GenerateFullKey(key);

            string pair = null;

            if (this.IsExtendingLifeTimeUponGet)
            {
                var tx = CacheFactory.GetInstance().CreateTransaction();
                tx.KeyExpireAsync(key, this.DefaultLifeTime);
                var pairTask = tx.StringGetAsync(fullKey);
                tx.Execute();
                pair = pairTask.Result;
            }
            else
            {
                pair = CacheFactory.GetInstance().StringGet(fullKey);
            }
             

            if (string.IsNullOrEmpty(pair))
                return null;

            if (!pair.Contains("ArrayCollumn"))
                return pair;
                
            var table = JsonConvert.DeserializeObject<MyTable>(pair);
            try { 
                return (object[,])table.GetArray();
            }
            catch (Exception) { 
                return (object[])table.GetArray();
            }
        }


        public void Add(string key, object value)
        {
            Add(key, value, DefaultLifeTime);
        }

        private void Add(string key, object value, TimeSpan LifeTime)
        {
            object valueToAdd = value?.ToString() ?? string.Empty;
            var fullKey = GenerateFullKey(key);

            if (value != null && value.GetType().IsArray)
            {
                try
                {
                    var array = (object[,])value;

                    var table = new MyTable(array);
                    valueToAdd = JsonConvert.SerializeObject(table);
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("cast object", StringComparison.InvariantCultureIgnoreCase) > 0) //most likely the array is not bi-dimensional, try again with only 1 dimenion
                    {
                        var array = (object[])value;

                        var table = new MyTable(array);
                        valueToAdd = JsonConvert.SerializeObject(table);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (LifeTime.TotalMilliseconds > 0 )
            {
                CacheFactory.GetInstance().StringSet(fullKey, (string)valueToAdd, LifeTime);
            }
            else
            {
                CacheFactory.GetInstance().StringSet(fullKey, (string)valueToAdd);
            }
        }

        public object this[string key]
        {
            get { return Get(key); }
            set { Add(key, value); }
        }

        public void Init(string cacheId)
        {
            _storePrefix = string.Concat(cacheId, ":");
        }

        private string GenerateFullKey(string key)
        {
            if (string.IsNullOrEmpty(_storePrefix))
                throw new Exception("no cache key defined - operation not allowed.");

            return (string.Concat(_storePrefix, key));
        }

        public void Remove(string key)
        {
            CacheFactory.GetInstance().KeyDelete(GenerateFullKey(key));
        }

        public bool Exists(string key)
        {
            return CacheFactory.GetInstance().KeyExists(GenerateFullKey(key));
        }
    }
}