﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DaxStudio.UI.Utils;

namespace DaxStudio.Tests
{
    [TestClass]
    public class DaxLineParserTests
    {
        [TestMethod]
        public void TestParseLineWithOpenColumn()
        {
            Assert.AreEqual(LineState.Column, DaxLineParser.ParseLine("table[column",12).LineState);
        }

        [TestMethod]
        public void TestParseLineWithOpenColumnAndPreceedingString()
        {
            var testText = "\"table[column\" 'table";
            Assert.AreEqual(LineState.Table, DaxLineParser.ParseLine(testText, testText.Length-1).LineState,"Table state not detected");
            Assert.AreEqual(LineState.String, DaxLineParser.ParseLine(testText, 10).LineState,"string state not detected");
        }

        [TestMethod]
        public void TestParseLineWithOpenTable()
        {
            Assert.AreEqual(LineState.Table, DaxLineParser.ParseLine("'table",6).LineState);
        }

        [TestMethod]
        public void TestFindTableNameSimple()
        {
            Assert.AreEqual("table", DaxLineParser.GetPreceedingTableName("filter( table"));
            Assert.AreEqual("table2", DaxLineParser.GetPreceedingTableName("evaluate filter( table2"));
        }

        [TestMethod]
        public void TestFindTableNameFunctionNoSpace()
        {
            Assert.AreEqual("table", DaxLineParser.GetPreceedingTableName("filter(table"));
        }

        [TestMethod]
        public void TestFindTableNameFunctionNoSpaceAndOperator()
        {
            Assert.AreEqual("table2", DaxLineParser.GetPreceedingTableName("filter(table, table1[col1]=table2"));
        }

        [TestMethod]
        public void TestFindTableNameQuotedFunctionNoSpaceAndOperator()
        {
            Assert.AreEqual("table2", DaxLineParser.GetPreceedingTableName("filter(table, table1[col1]='table2"));
        }

        [TestMethod]
        public void TestFindTableNameFunctionNoSpaceAndEvaluate()
        {
            Assert.AreEqual("table", DaxLineParser.GetPreceedingTableName("evaluate filter(table"));
        }
        
        [TestMethod]
        public void GetCompletionSegmentTest()
        {
            var daxState = DaxLineParser.ParseLine("table[column]",10);
            Assert.AreEqual(LineState.Column, daxState.LineState );
            Assert.AreEqual(5, daxState.StartOffset, "StartOffset");
            Assert.AreEqual(12, daxState.EndOffset, "EndOffset");
        }

        [TestMethod]
        public void GetCompletionSegmentTestWithLeadingAndTrailingText()
        {
            var dax = "filter( table[column] , table[column]=\"red\"";
            //                       ^ 15                       ^ 41
            var daxState = DaxLineParser.ParseLine(dax, 15); 
            Assert.AreEqual(LineState.Column, daxState.LineState);
            Assert.AreEqual(13, daxState.StartOffset, "StartOffset");
            Assert.AreEqual(20, daxState.EndOffset, "EndOffset");
            Assert.AreEqual("table", daxState.TableName);
            
            var daxState2 = DaxLineParser.ParseLine(dax, 41);
            Assert.AreEqual(LineState.String, daxState2.LineState);
            Assert.AreEqual(38, daxState2.StartOffset, "StartOffset String");
            Assert.AreEqual(42, daxState2.EndOffset, "EndOffset String");
            
            var daxState3 = DaxLineParser.ParseLine(dax, 3);
            Assert.AreEqual(LineState.LetterOrDigit, daxState3.LineState);
            Assert.AreEqual(0, daxState3.StartOffset, "StartOffset Filter");
            Assert.AreEqual(6, daxState3.EndOffset, "EndOffset Filter");

        }

        [TestMethod]
        public void GetCompletionSegmentTestWithQuatedTableName()
        {
            var dax = "evaluate filter('my table', 'my table'[column name";
            //                                                ^ 39
            var daxState = DaxLineParser.ParseLine(dax, dax.Length-1);
            Assert.AreEqual(LineState.Column, daxState.LineState);
            Assert.AreEqual(38, daxState.StartOffset, "StartOffset");
            Assert.AreEqual(dax.Length-1, daxState.EndOffset, "EndOffset");
            Assert.AreEqual("my table", daxState.TableName);
        }
    }
}
