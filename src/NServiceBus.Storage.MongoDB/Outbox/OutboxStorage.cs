﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxStorage : Feature
    {
        OutboxStorage()
        {
            DependsOn<Features.Outbox>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions) && useTransactions == false)
            {
                throw new Exception($"Transactions are required when the Outbox is enabled, but they have been disabled by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

            if (!BsonClassMap.IsClassMapRegistered(typeof(TransportOperation)))
            {
                BsonClassMap.RegisterClassMap<TransportOperation>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(c => c.Headers).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                    cm.MapMember(c => c.Options).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                });
            }

            if (!context.Settings.TryGet(SettingsKeys.OutboxTimeSpan, out TimeSpan outboxExpiresAfter))
            {
                outboxExpiresAfter = TimeSpan.FromDays(7);
            }

            var outboxCollection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)));

            var outboxCleanupIndex = outboxCollection.Indexes.List().ToList().SingleOrDefault(indexDocument => indexDocument.GetElement("name").Value == "OutboxCleanup");

            if (outboxCleanupIndex == null)
            {
                var indexModel = new CreateIndexModel<OutboxRecord>(Builders<OutboxRecord>.IndexKeys.Ascending(m => m.Dispatched), new CreateIndexOptions
                {
                    ExpireAfter = outboxExpiresAfter,
                    Name = "OutboxCleanup",
                    Background = true
                });

                outboxCollection.Indexes.CreateOne(indexModel);
            }
            else
            {
                var existingValue = outboxCleanupIndex.GetElement("expireAfterSeconds").Value.ToInt32();
                if (TimeSpan.FromSeconds(existingValue) != outboxExpiresAfter)
                {
                    outboxCollection.Indexes.DropOne("OutboxCleanup");

                    var indexModel = new CreateIndexModel<OutboxRecord>(Builders<OutboxRecord>.IndexKeys.Ascending(m => m.Dispatched), new CreateIndexOptions
                    {
                        ExpireAfter = outboxExpiresAfter,
                        Name = "OutboxCleanup",
                        Background = true
                    });

                    outboxCollection.Indexes.CreateOne(indexModel);
                }
            }

            context.Container.ConfigureComponent(() => new OutboxPersister(client, databaseName, collectionNamingConvention), DependencyLifecycle.SingleInstance);
        }
    }
}
