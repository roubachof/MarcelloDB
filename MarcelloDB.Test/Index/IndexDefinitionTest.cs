﻿    using System;
using NUnit.Framework;
using MarcelloDB.Test.Classes;
using MarcelloDB.Index;
using System.Collections.Generic;
using MarcelloDB.Collections;
using System.Linq;
using MarcelloDB.Records;

namespace MarcelloDB.Test.Index
{
    class TestDefinition : IndexDefinition<Article>
    {
        public IndexedValue<Article, string> Name{ get; set; }

        public IndexedValue<Article, string> CustomDescription
        {
            get
            {
                return IndexedValue((Article article)=>{
                    return "Custom" + article.Description;
                });
            }
        }
    }

    [TestFixture]
    public class IndexDefinitionTest
    {
        TestDefinition _definition;

        [SetUp]
        public void Initialize()
        {
            _definition = new TestDefinition();
            _definition.Initialize();
        }

        [Test]
        public void IndexedValues_Contains_IndexedValue_For_Empty_Property()
        {
            var indexedValues = _definition.IndexedValues;
            Assert.NotNull(indexedValues.FirstOrDefault(v => v.PropertyName == "Name"));
        }

        [Test]
        public void IndexedValues_Contains_Indexed_Value_For_Implemented_Property()
        {
            var indexedValues = _definition.IndexedValues;
            Assert.NotNull(indexedValues.FirstOrDefault(v => v.PropertyName == "CustomDescription"));
        }

        [Test]
        public void IndexedField_GetKey_Returns_Correct_Key_For_Empty_Property()
        {
            var indexedValues = _definition.IndexedValues;
            var indexKey = (ValueWithAddressIndexKey<string>) indexedValues.FirstOrDefault(v => v.PropertyName == "Name")
                .GetKeys(Article.BarbieDoll, 123).First();
            Assert.AreEqual(Article.BarbieDoll.Name, indexKey.V);
            Assert.AreEqual(123, indexKey.A);
        }

        [Test]
        public void IndexedField_GetKey_Returns_Correct_Key_For_Custom_Property()
        {
            var indexedValues = _definition.IndexedValues;
            var indexKey = (ValueWithAddressIndexKey<string>) indexedValues.FirstOrDefault(v => v.PropertyName == "CustomDescription")
                .GetKeys(Article.BarbieDoll, 123).First();
            Assert.AreEqual("Custom" + Article.BarbieDoll.Description, indexKey.V);
            Assert.AreEqual(123, indexKey.A);
        }
    }
}

