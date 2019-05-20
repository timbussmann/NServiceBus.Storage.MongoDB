﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class StorageSession : CompletableSynchronizedStorageSession
    {
        public StorageSession(IMongoDatabase database, ContextBag contextBag)
        {
            this.database = database;
            this.contextBag = contextBag;
        }

        public IMongoCollection<BsonDocument> GetCollection(Type type) => database.GetCollection<BsonDocument>(GetCollectionName(type)).WithReadPreference(ReadPreference.Primary).WithWriteConcern(WriteConcern.WMajority);

        public void StoreVersion(Type type, BsonValue version) => contextBag.Set(type.FullName, version);

        public BsonValue RetrieveVersion(Type type) => contextBag.Get<BsonValue>(type.FullName);

        public T Deserialize<T>(BsonDocument doc)
        {
            if (doc == null)
            {
                return default(T);
            }

            return BsonSerializer.Deserialize<T>(doc);
        }

        public Task CompleteAsync()
        {
            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {

        }

        protected string GetCollectionName(Type entityType)
        {
            return entityType.Name.ToLower();
        }

        readonly IMongoDatabase database;
        readonly ContextBag contextBag;
    }
}
