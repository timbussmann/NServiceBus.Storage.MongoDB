﻿using System;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    public class MongoDBPersistence : PersistenceDefinition
    {
        static IMongoClient defaultClient;

        public MongoDBPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();

                s.SetDefault(SettingsKeys.Client, (Func<IMongoClient>)(() => {
                    if (defaultClient == null)
                    {
                        defaultClient = new MongoClient();
                    }
                    return defaultClient;
                }));

                s.SetDefault(SettingsKeys.DatabaseName, s.EndpointName());
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
        }
    }
}