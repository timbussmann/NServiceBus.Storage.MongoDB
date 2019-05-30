﻿using System;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoPersistence : PersistenceDefinition
    {
        static IMongoClient defaultClient;

        /// <summary>
        /// 
        /// </summary>
        public MongoPersistence()
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