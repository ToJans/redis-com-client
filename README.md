# redis-com-client
Redis Client for COM+ | StackExchange.Redis Wrapper

This was made to be used on Classic ASP (ASP 3.0).

Line command to install the COM+: 

```cmd
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\regasm redis-com-client.dll /tlb:redis-com-client.tlb /codebase
```

On the ASP side you have not create the object.
I initialized this in the `global.asa`.

```asp
<OBJECT RUNAT=Server SCOPE=Application ID=Cache PROGID=CacheManager></OBJECT>
```

Later you can use these operations:

## Initialize

This operation is required in order to share the same Redis instance with N sites.

```VBScript
  Cache.Init "prefix1"
```

## Add
  
```VBScript
  Cache.Add "key1", "value"
```

  or

```VBScript
  Cache("key1") = "value"
```

## Add with expiration

```VBScript
  Cache.SetExpiration "key1", "value", 1000 'ms
```
  
## Get

```VBScript
  Cache.Get "key1"
```

  or
  
```VBScript
  Cache("key1")
```

## Remove
```VBScript
  Cache.Remove "key1"
```
  
## Remove All
```VBScript
  Cache.RemoveAll()
```
  
