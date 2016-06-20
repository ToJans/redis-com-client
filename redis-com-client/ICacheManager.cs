using System;
using System.Runtime.InteropServices;

namespace redis_com_client
{
    [ComVisible(true)]
    [Guid("A64CEB93-6462-487F-8503-D7D891D14687")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ICacheManager
    {
        void Add(string key, object value);
        object Get(string key);
        void RemoveAll();
        object this[string key] { get; set; }
        void Init(string cacheId);
        void Remove(string key);
        bool Exists(string key);
        void SetExpiration(string key, TimeSpan lifeTime);
        TimeSpan DefaultLifeTime { get; set; }
        bool IsExtendingLifeTimeUponGet { get; set; }
    }
}