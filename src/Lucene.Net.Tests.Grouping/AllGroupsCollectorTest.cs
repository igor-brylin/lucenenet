﻿using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Lucene.Net.Search.Grouping.Terms;

namespace Lucene.Net.Search.Grouping
{
    public class AllGroupsCollectorTest : LuceneTestCase
    {
        [Test]
        public void TestTotalGroupCount()
        {

            string groupField = "author";
            FieldType customType = new FieldType();
            customType.Stored = true;

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".equals(w.w.Config.Codec.Name);

            // 0
            Document doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            doc.Add(new Field("id", "1", customType));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random text blob", Field.Store.YES));
            doc.Add(new Field("id", "2", customType));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.YES));
            doc.Add(new Field("id", "3", customType));
            w.AddDocument(doc);
            w.Commit(); // To ensure a second segment

            // 3
            doc = new Document();
            AddGroupField(doc, groupField, "author2", canUseIDV);
            doc.Add(new TextField("content", "some random text", Field.Store.YES));
            doc.Add(new Field("id", "4", customType));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "5", customType));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "random blob", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            // 6 -- no author field
            doc = new Document();
            doc.Add(new TextField("content", "random word stuck in alot of other text", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.Reader);
            w.Dispose();

            AbstractAllGroupsCollector allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupsCollector);
            assertEquals(4, allGroupsCollector.GroupCount);

            allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "some")), allGroupsCollector);
            assertEquals(3, allGroupsCollector.GroupCount);

            allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "blob")), allGroupsCollector);
            assertEquals(2, allGroupsCollector.GroupCount);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(groupField, new BytesRef(value)));
            }
        }

        private AbstractAllGroupsCollector CreateRandomCollector(string groupField, bool canUseIDV)
        {
            AbstractAllGroupsCollector selected;
            if (Random().nextBoolean())
            {
                selected = new TermAllGroupsCollector(groupField);
            }
            else
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                selected = new FunctionAllGroupsCollector(vs, new Hashtable());
            }

            if (VERBOSE)
            {
                Console.WriteLine("Selected implementation: " + selected.GetType().Name);
            }

            return selected;
        }

    }
}