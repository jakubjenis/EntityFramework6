﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Resources;
    using System.Data.Entity.SqlServer;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using Xunit;

    public class StoreItemCollectionTests
    {
        private const string Ssdl = 
            "<Schema Namespace='AdventureWorksModel.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>" +
            "  <EntityContainer Name='AdventureWorksModelStoreContainer'>" +
            "    <EntitySet Name='Entities' EntityType='AdventureWorksModel.Store.Entities' Schema='dbo' />" +
            "  </EntityContainer>" +
            "  <EntityType Name='Entities'>" +
            "    <Key>" +
            "      <PropertyRef Name='Id' />" +
            "    </Key>" +
            "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />" +
            "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />" +
            "  </EntityType>" +
            "</Schema>";


        [Fact]
        public void StoreItemCollection_Create_factory_method_throws_for_null_readers()
        {
            IList<EdmSchemaError> errors;

            Assert.Equal("xmlReaders",
                Assert.Throws<ArgumentNullException>(
                    () => StoreItemCollection.Create(null, null, out errors)).ParamName);
        }

        [Fact]
        public void StoreItemCollection_Create_factory_method_throws_for_empty_reader_collection()
        {
            IList<EdmSchemaError> errors;

            Assert.Equal(Strings.StoreItemCollectionMustHaveOneArtifact("xmlReaders"),
                Assert.Throws<ArgumentException>(
                    () => StoreItemCollection.Create(new XmlReader[0], null, out errors)).Message);
            
        }

        [Fact]
        public void StoreItemCollection_Create_factory_method_throws_for_null_reader_in_the_collection()
        {
            IList<EdmSchemaError> errors;

            Assert.Equal(Strings.CheckArgumentContainsNullFailed("xmlReaders"),
                Assert.Throws<ArgumentException>(
                    () => StoreItemCollection.Create(new XmlReader[1], null, out errors)).Message);
        }

        [Fact]
        public void StoreItemCollection_Create_factory_method_returns_null_and_errors_for_invalid_ssdl()
        {
            var invalidSsdl = XDocument.Parse(Ssdl);
            invalidSsdl
                .Descendants("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}EntityType")
                .Remove();

            IList<EdmSchemaError> errors;
            var storeItemCollection = StoreItemCollection.Create(new[] { invalidSsdl.CreateReader() }, null, out errors);

            Assert.Null(storeItemCollection);
            Assert.Equal(1, errors.Count);
            Assert.Contains("Entities", errors[0].Message);
        }

        [Fact]
        public void StoreItemCollection_Create_factory_method_returns_StoreItemCollection_instance_for_valid_ssdl()
        {
            IList<EdmSchemaError> errors;
            var storeItemCollection = StoreItemCollection.Create(new[] { XDocument.Parse(Ssdl).CreateReader() }, null, out errors);

            Assert.NotNull(storeItemCollection);
            Assert.NotNull(storeItemCollection.GetItem<EntityType>("AdventureWorksModel.Store.Entities"));
            Assert.Equal(0, errors.Count);
        }
        
        [Fact]
        public void Can_initialize_from_ssdl_model_and_items_set_read_only_and_added_to_collection()
        {
            var context = new ShopContext_v1();
            var compiledModel = context.InternalContext.CodeFirstModel;

            var builder = compiledModel.CachedModelBuilder.Clone();

            var databaseMapping
                = builder.Build(ProviderRegistry.Sql2008_ProviderInfo).DatabaseMapping;

            var storeItemCollection = new StoreItemCollection(databaseMapping.Database);

            Assert.Equal(3.0, storeItemCollection.StoreSchemaVersion);

            foreach (var globalItem in databaseMapping.Database.GlobalItems)
            {
                Assert.True(storeItemCollection.Contains(globalItem));
                Assert.True(globalItem.IsReadOnly);
            }
        }

        [Fact]
        public void Can_initialize_and_provider_fields_set()
        {
            var model 
                = new EdmModel
                            {
                                ProviderInfo = ProviderRegistry.Sql2008_ProviderInfo,
                                ProviderManifest = new SqlProviderManifest("2008"),
                                Version = 3.0
                            };

            var itemCollection = new StoreItemCollection(model);

            Assert.Same(model.ProviderInfo.ProviderManifestToken, itemCollection.StoreProviderManifestToken);
            Assert.NotNull(itemCollection.StoreProviderFactory);
            Assert.Same(model.ProviderManifest, itemCollection.StoreProviderManifest);
        }
    }
}