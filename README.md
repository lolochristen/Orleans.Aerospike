
# Orleans Aerospike Providers
[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[Aerospike](https://www.aerospike.com/) Aerospike is a flash-optimized in-memory open source NoSQL database and the name of the eponymous company that produces it. The combination of in-memory and storage capabilities makes it ideal to use for scaled Orleans environments. 

**Orleans.Aerospike** is a package that use Aerospike as a backend for Orleans providers like Cluster Membership, Grain State storage and Reminders. 

# Installation
Nuget Packages are provided:

*Orleans.Clustering.Aerospike*

*Orleans.Persistence.Aerospike*

*Orleans.Reminder.Aerospike*

# Configuration

It is not mandatory to use all the providers at once. Just pick the one you are interested in from the samples and you should be good as they don't depend on each other.

## Silo

***Example***
```cs

var silo = new SiloHostBuilder()
    .UseAerospikeMembership(options => {   }) 
    .AddAerospikeGrainStorageAsDefault(options => {   }) 
    .UseAerospikeReminder(options => {   })
    .Build();
await silo.StartAsync();
```

## Client

***Example***
```cs
var clientConfig = new ClientConfiguration();

var client = new ClientBuilder()
    .UseAerospikeGatewayListProvider(opt => 
    {
    }) 
    .Build();
    await client.Connect();
```

## Serializer
Different Providers are available (Orleans.Persistence.Aerospike.Serializer) to define how the state shall be stored in a Aerospike data set:

| Serializer | Behavior |
| ---- | ---- |
| AerospikeOrleansSerializer | Serializes state using default serializer for Orleans (SerializerManager) |
| AerospikeMessagePackSerializer | Serializes state using MessagePack format |
| AerospikeJsonSerializer | Serializes state to Json |
| AerospikePropertySerializer | Serializes each root property to explicit bin. Complex are serialized as Json. This makes state visible and queriable in the database.   |


***Example***
```cs
var silo = new SiloHostBuilder()
    .AddAerospikeGrainStorageAsDefault(options => {   }) 
    .UseAerospikeSerializer<AerospikePropertySerializer>();
    .Build();
await silo.StartAsync();
```


